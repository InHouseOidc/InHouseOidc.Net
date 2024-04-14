// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Extension;
using InHouseOidc.Common.Type;
using InHouseOidc.Discovery;
using InHouseOidc.PageClient.Type;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InHouseOidc.PageClient.Resolver
{
    internal class PageAccessTokenResolver(
        ClientOptions clientConfiguration,
        IDiscoveryResolver discoveryResolver,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PageAccessTokenResolver> logger,
        IUtcNow utcNow
    ) : IPageAccessTokenResolver
    {
        private readonly ClientOptions clientOptions = clientConfiguration;
        private readonly IDiscoveryResolver discoveryResolver = discoveryResolver;
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
        private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
        private readonly ILogger<PageAccessTokenResolver> logger = logger;
        private readonly IUtcNow utcNow = utcNow;

        public async Task<string?> GetClientToken(string clientName, CancellationToken cancellationToken)
        {
            // We'll need to be able to get tokens from an active HttpContext
            if (this.httpContextAccessor.HttpContext == null)
            {
                return null;
            }
            var authenticateResult = await this.httpContextAccessor.HttpContext.AuthenticateAsync();
            if (authenticateResult == null || !authenticateResult.Succeeded || authenticateResult.Properties == null)
            {
                this.logger.LogInformation("GetClientToken failed. User not authenticated.");
                return null;
            }
            // Get any current access token and it's expiry
            var tokens = authenticateResult.Properties.GetTokens();
            var accessToken = tokens.FirstOrDefault(t => t.Name == JsonWebTokenConstant.AccessToken);
            var accessTokenExpiry = tokens.FirstOrDefault(t => t.Name == PageConstant.ExpiresAt);
            if (accessToken != null && accessTokenExpiry != null)
            {
                var expiry = DateTimeOffset.Parse(accessTokenExpiry.Value).ToUniversalTime();
                if (expiry > this.utcNow.UtcNow)
                {
                    // Still good to use
                    return accessToken.Value;
                }
            }
            // We'll need a reference token to obtain another access token
            var refreshToken = tokens.FirstOrDefault(t => t.Name == JsonWebTokenConstant.RefreshToken);
            if (refreshToken == null)
            {
                this.logger.LogInformation("GetClientToken failed. Refresh token not found.");
                return null;
            }
            // Lookup configuration options
            if (
                this.clientOptions.PageClientOptions == null
                || string.IsNullOrEmpty(this.clientOptions.PageClientOptions.ClientId)
                || string.IsNullOrEmpty(this.clientOptions.PageClientOptions.Scope)
                || string.IsNullOrEmpty(this.clientOptions.PageClientOptions.OidcProviderAddress)
            )
            {
                // Can't wait for records with required initialisers to be added to C#
                this.logger.LogError("PageClientOptions incorrectly configured");
                return null;
            }
            // Access discovery
            var discovery = await this.discoveryResolver.GetDiscovery(
                this.clientOptions.DiscoveryOptions,
                this.clientOptions.PageClientOptions.OidcProviderAddress,
                CancellationToken.None
            );
            if (discovery == null)
            {
                return null;
            }
            // Request a new access token using the current refresh token
            var httpClient = this.httpClientFactory.CreateClient(this.clientOptions.InternalHttpClientName);
            var providerUri = new Uri(
                this.clientOptions.PageClientOptions.OidcProviderAddress.EnsureEndsWithSlash(),
                UriKind.Absolute
            );
            var tokenEndpointUri = new Uri(providerUri, discovery.TokenEndpoint);
            var form = new Dictionary<string, string>
            {
                { TokenEndpointConstant.ClientId, this.clientOptions.PageClientOptions.ClientId },
                { TokenEndpointConstant.GrantType, TokenEndpointConstant.RefreshToken },
                { TokenEndpointConstant.RefreshToken, refreshToken.Value },
                { TokenEndpointConstant.Scope, this.clientOptions.PageClientOptions.Scope },
            };
            var formContent = new FormUrlEncodedContent(form);
            var response = await httpClient.SendWithRetry(
                HttpMethod.Post,
                tokenEndpointUri,
                formContent,
                this.logger,
                cancellationToken,
                this.clientOptions.MaxRetryAttempts,
                this.clientOptions.RetryDelayMilliseconds
            );
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError(
                    "Unable to get access token from {oidcProviderAddress} for client {clientName} response {statusCode}",
                    this.clientOptions.PageClientOptions.OidcProviderAddress,
                    clientName,
                    response.StatusCode
                );
                return null;
            }
            var tokenResponse = await response.Content.ReadJsonAs<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                this.logger.LogError(
                    "Invalid token from {oidcProviderAddress} for client {clientName}",
                    this.clientOptions.PageClientOptions.OidcProviderAddress,
                    clientName
                );
                return null;
            }
            // Store the updated tokens in the authentication cookie & reissue the cookie
            var updatedExpiry = this.utcNow.UtcNow.UtcDateTime.AddSeconds(tokenResponse.ExpiresIn ?? 0);
            authenticateResult.Properties.Items[$".Token.{JsonWebTokenConstant.AccessToken}"] =
                tokenResponse.AccessToken;
            authenticateResult.Properties.Items[$".Token.{PageConstant.ExpiresAt}"] = updatedExpiry.ToString("o");
            authenticateResult.Properties.Items[$".Token.{JsonWebTokenConstant.RefreshToken}"] =
                tokenResponse.RefreshToken;
            authenticateResult.Properties.IssuedUtc = null;
            await this.httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                authenticateResult.Principal,
                authenticateResult.Properties
            );
            return tokenResponse.AccessToken;
        }
    }
}
