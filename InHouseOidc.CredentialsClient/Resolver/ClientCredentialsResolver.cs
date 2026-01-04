// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Extension;
using InHouseOidc.Common.Type;
using InHouseOidc.CredentialsClient.Type;
using InHouseOidc.Discovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InHouseOidc.CredentialsClient.Resolver
{
    internal class ClientCredentialsResolver(
        IAsyncLock<ClientCredentialsResolver> asyncLock,
        ClientOptions clientConfiguration,
        IDiscoveryResolver discoveryResolver,
        IHttpClientFactory httpClientFactory,
        ILogger<ClientCredentialsResolver> logger,
        IServiceProvider serviceProvider,
        IUtcNow utcNow
    ) : IClientCredentialsResolver
    {
        private readonly IAsyncLock<ClientCredentialsResolver> asyncLock = asyncLock;
        private readonly ClientOptions clientOptions = clientConfiguration;
        private readonly IDiscoveryResolver discoveryResolver = discoveryResolver;
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
        private readonly ILogger<ClientCredentialsResolver> logger = logger;
        private readonly IServiceProvider serviceProvider = serviceProvider;
        private readonly IUtcNow utcNow = utcNow;
        private readonly ConcurrentDictionary<string, ClientCredentialsToken> tokenDictionary = new();

        public Task ClearClientToken(string clientName)
        {
            this.tokenDictionary.TryRemove(clientName, out var _);
            return Task.CompletedTask;
        }

        public async Task<string?> GetClientToken(string clientName, CancellationToken cancellationToken = default)
        {
            // Check for already resolved
            if (this.tokenDictionary.TryGetValue(clientName, out var token))
            {
                if (token.ExpiryUtc > this.utcNow.UtcNow)
                {
                    return token.AccessToken;
                }
            }
            // Lookup the client credentials options
            this.clientOptions.CredentialsClientsOptions.TryGetValue(clientName, out var credentialsClientOptions);
            if (credentialsClientOptions == null)
            {
                // Retrieve the options and save the value for all subsequent token requests
                var credentialsStore = this.serviceProvider.GetService<ICredentialsStore>();
                if (credentialsStore == null)
                {
                    this.logger.LogError("Client credentials options not available via AddClient or ICredentialsStore");
                    return null;
                }
                credentialsClientOptions = await credentialsStore.GetCredentialsClientOptions(clientName);
                if (credentialsClientOptions == null)
                {
                    this.logger.LogError("Client credentials options not available from ICredentialsStore");
                    return null;
                }
                this.clientOptions.CredentialsClientsOptions[clientName] = credentialsClientOptions;
            }
            return await this.GetClientTokenInternal(clientName, credentialsClientOptions, cancellationToken);
        }

        public async Task<string?> GetClientToken(
            string clientName,
            CredentialsClientOptions credentialsClientOptions,
            CancellationToken cancellationToken = default
        )
        {
            // Check for already resolved
            if (this.tokenDictionary.TryGetValue(clientName, out var token))
            {
                if (token.ExpiryUtc > this.utcNow.UtcNow)
                {
                    return token.AccessToken;
                }
            }
            return await this.GetClientTokenInternal(clientName, credentialsClientOptions, cancellationToken);
        }

        private async Task<string?> GetClientTokenInternal(
            string clientName,
            CredentialsClientOptions credentialsClientOptions,
            CancellationToken cancellationToken
        )
        {
            // Confirm required client options fields are present
            if (
                string.IsNullOrEmpty(credentialsClientOptions.ClientId)
                || string.IsNullOrEmpty(credentialsClientOptions.ClientSecret)
                || string.IsNullOrEmpty(credentialsClientOptions.OidcProviderAddress)
                || string.IsNullOrEmpty(credentialsClientOptions.Scope)
            )
            {
                throw new InvalidOperationException("Client options are missing required values");
            }
            // Lock to prevent multiple token requests for the same client
            using var locker = this.asyncLock.Lock();
            if (
                this.tokenDictionary.TryGetValue(clientName, out var existingToken)
                && existingToken.ExpiryUtc > this.utcNow.UtcNow
            )
            {
                // Another thread has already obtained the token while we waited
                return existingToken.AccessToken;
            }
            // Use discovery to resolve the token endpoint
            var discovery = await this.discoveryResolver.GetDiscovery(
                this.clientOptions.DiscoveryOptions,
                credentialsClientOptions.OidcProviderAddress,
                cancellationToken
            );
            if (discovery == null)
            {
                // Discovery resolution failed, error will have already been logged
                return null;
            }
            // Verify the OP supports client credentials form post
            if (!discovery.TokenEndpointAuthMethodsSupported.Contains(DiscoveryConstant.ClientSecretPost))
            {
                this.logger.LogError("Provider does not support client_secret_post auth method");
                return null;
            }
            // Create the endpoint URIs
            var providerUri = new Uri(
                credentialsClientOptions.OidcProviderAddress.EnsureEndsWithSlash(),
                UriKind.Absolute
            );
            var tokenEndpointUri = new Uri(providerUri, discovery.TokenEndpoint);
            // Request a token
            var httpClient = this.httpClientFactory.CreateClient(this.clientOptions.InternalHttpClientName);
            var form = new Dictionary<string, string>
            {
                { TokenEndpointConstant.GrantType, TokenEndpointConstant.ClientCredentials },
                { TokenEndpointConstant.ClientId, credentialsClientOptions.ClientId },
                { TokenEndpointConstant.ClientSecret, credentialsClientOptions.ClientSecret },
                { TokenEndpointConstant.Scope, credentialsClientOptions.Scope },
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
                    "Unable to obtain token from {oidcProviderAddress} response {statusCode}",
                    credentialsClientOptions.OidcProviderAddress,
                    response.StatusCode
                );
                return null;
            }
            var tokenResponse = await response.Content.ReadJsonAs<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                this.logger.LogError(
                    "No token returned from {oidcProviderAddress} for client {clientName}",
                    credentialsClientOptions.OidcProviderAddress,
                    clientName
                );
                return null;
            }
            // Cache the value
            var token = new ClientCredentialsToken(
                tokenResponse.AccessToken,
                this.utcNow.UtcNow.AddSeconds(tokenResponse.ExpiresIn ?? 0)
            );
            this.tokenDictionary[clientName] = token;
            return token.AccessToken;
        }
    }
}
