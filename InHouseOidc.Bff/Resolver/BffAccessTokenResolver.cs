// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Type;
using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Extension;
using InHouseOidc.Common.Type;
using InHouseOidc.Discovery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace InHouseOidc.Bff.Resolver
{
    internal class BffAccessTokenResolver(
        IBffClientResolver bffClientResolver,
        ClientOptions clientOptions,
        IDiscoveryResolver discoveryResolver,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BffAccessTokenResolver> logger,
        IUtcNow utcNow
    ) : IBffAccessTokenResolver
    {
        public async Task<string?> GetClientToken(string clientName, CancellationToken cancellationToken)
        {
            // We'll need to be able to get tokens from an active HttpContext
            if (httpContextAccessor.HttpContext == null)
            {
                return null;
            }
            var authenticateResult = await httpContextAccessor.HttpContext.AuthenticateAsync(
                BffConstant.AuthenticationSchemeCookie
            );
            if (authenticateResult == null || !authenticateResult.Succeeded || authenticateResult.Properties == null)
            {
                logger.LogInformation("GetClientToken failed. User not authenticated.");
                return null;
            }
            // Get any current access token and it's expiry
            var tokens = authenticateResult.Properties.GetTokens();
            var accessToken = tokens.FirstOrDefault(t => t.Name == JsonWebTokenConstant.AccessToken);
            var accessTokenExpiry = tokens.FirstOrDefault(t => t.Name == BffConstant.ExpiresAt);
            if (accessToken != null && accessTokenExpiry != null)
            {
                var expiry = DateTimeOffset.Parse(accessTokenExpiry.Value).ToUniversalTime();
                if (expiry > utcNow.UtcNow)
                {
                    // Still good to use
                    return accessToken.Value;
                }
            }
            // We'll need a reference token to obtain another access token
            var refreshToken = tokens.FirstOrDefault(t => t.Name == JsonWebTokenConstant.RefreshToken);
            if (refreshToken == null)
            {
                logger.LogInformation("GetClientToken failed. Refresh token not found.");
                return null;
            }
            var (bffClientOptions, scheme) = bffClientResolver.GetClient(httpContextAccessor.HttpContext);
            // Access discovery
            var discovery = await discoveryResolver.GetDiscovery(
                clientOptions.DiscoveryOptions,
                bffClientOptions.OidcProviderAddress,
                CancellationToken.None
            );
            if (discovery == null)
            {
                return null;
            }
            // Request a new access token using the current refresh token
            var httpClient = httpClientFactory.CreateClient(clientOptions.InternalHttpClientName);
            var providerUri = new Uri(bffClientOptions.OidcProviderAddress.EnsureEndsWithSlash(), UriKind.Absolute);
            var tokenEndpointUri = new Uri(new Uri(bffClientOptions.OidcProviderAddress), discovery.TokenEndpoint);
            var form = new Dictionary<string, string>
            {
                { TokenEndpointConstant.ClientId, bffClientOptions.ClientId },
                { TokenEndpointConstant.GrantType, TokenEndpointConstant.RefreshToken },
                { TokenEndpointConstant.RefreshToken, refreshToken.Value },
            };
            if (!string.IsNullOrEmpty(bffClientOptions.Scope))
            {
                form.Add(TokenEndpointConstant.Scope, bffClientOptions.Scope);
            }
            var formContent = new FormUrlEncodedContent(form);
            var response = await httpClient.SendWithRetry(
                HttpMethod.Post,
                tokenEndpointUri,
                formContent,
                logger,
                cancellationToken,
                clientOptions.MaxRetryAttempts,
                clientOptions.RetryDelayMilliseconds
            );
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Unable to get access token from {oidcProviderAddress} for client {clientName} response {statusCode}",
                    bffClientOptions.OidcProviderAddress,
                    clientName,
                    response.StatusCode
                );
                return null;
            }
            var tokenResponse = await response.Content.ReadJsonAs<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                logger.LogError(
                    "Invalid token from {oidcProviderAddress} for client {clientName}",
                    bffClientOptions.OidcProviderAddress,
                    clientName
                );
                return null;
            }
            // Store the updated tokens in the authentication cookie & reissue the cookie
            var updatedExpiry = utcNow.UtcNow.UtcDateTime.AddSeconds(tokenResponse.ExpiresIn ?? 0);
            authenticateResult.Properties.Items[$".Token.{JsonWebTokenConstant.AccessToken}"] =
                tokenResponse.AccessToken;
            authenticateResult.Properties.Items[$".Token.{BffConstant.ExpiresAt}"] = updatedExpiry.ToString("o");
            authenticateResult.Properties.Items[$".Token.{JsonWebTokenConstant.RefreshToken}"] =
                tokenResponse.RefreshToken;
            authenticateResult.Properties.IssuedUtc = null;
            await httpContextAccessor.HttpContext.SignInAsync(
                BffConstant.AuthenticationSchemeCookie,
                authenticateResult.Principal,
                authenticateResult.Properties
            );
            return tokenResponse.AccessToken;
        }
    }
}
