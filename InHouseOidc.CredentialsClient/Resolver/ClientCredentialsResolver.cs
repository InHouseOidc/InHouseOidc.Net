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
using System.Collections.Concurrent;

namespace InHouseOidc.CredentialsClient.Resolver
{
    internal class ClientCredentialsResolver : IClientCredentialsResolver
    {
        private readonly ClientOptions clientOptions;
        private readonly IDiscoveryResolver discoveryResolver;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<ClientCredentialsResolver> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly IUtcNow utcNow;
        private readonly ConcurrentDictionary<string, ClientCredentialsToken> tokenDictionary;

        public ClientCredentialsResolver(
            ClientOptions clientConfiguration,
            IDiscoveryResolver discoveryResolver,
            IHttpClientFactory httpClientFactory,
            ILogger<ClientCredentialsResolver> logger,
            IServiceProvider serviceProvider,
            IUtcNow utcNow
        )
        {
            this.clientOptions = clientConfiguration;
            this.discoveryResolver = discoveryResolver;
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.utcNow = utcNow;
            this.tokenDictionary = new ConcurrentDictionary<string, ClientCredentialsToken>();
        }

        public Task ClearClientToken(string clientName)
        {
            this.tokenDictionary.TryRemove(clientName, out var _);
            return Task.CompletedTask;
        }

        public async Task<string?> GetClientToken(string clientName, CancellationToken cancellationToken)
        {
            // Check for already resolved
            if (this.tokenDictionary.TryGetValue(clientName, out var token))
            {
                if (token.ExpiryUtc > this.utcNow.UtcNow)
                {
                    return token.AccessToken;
                }
            }
            // Clear any existing token
            if (token != null)
            {
                this.tokenDictionary.TryRemove(clientName, out var _);
            }
            // Lookup the client credentials options
            this.clientOptions.CredentialsClientsOptions.TryGetValue(clientName, out var clientCredentialsOptions);
            if (clientCredentialsOptions == null)
            {
                // Retrieve the options and save the value for all subsequent token requests
                var credentialsStore = this.serviceProvider.GetService<ICredentialsStore>();
                if (credentialsStore == null)
                {
                    this.logger.LogError("Client credentials options not available via AddClient or ICredentialsStore");
                    return null;
                }
                clientCredentialsOptions = await credentialsStore.GetCredentialsClientOptions(clientName);
                if (clientCredentialsOptions == null)
                {
                    this.logger.LogError("Client credentials options not available from ICredentialsStore");
                    return null;
                }
                this.clientOptions.CredentialsClientsOptions[clientName] = clientCredentialsOptions;
            }
            // Confirm required client options fields are present
            if (
                string.IsNullOrEmpty(clientCredentialsOptions.ClientId)
                || string.IsNullOrEmpty(clientCredentialsOptions.ClientSecret)
                || string.IsNullOrEmpty(clientCredentialsOptions.OidcProviderAddress)
                || string.IsNullOrEmpty(clientCredentialsOptions.Scope)
            )
            {
                throw new InvalidOperationException("Client options are missing required values");
            }
            // Use discovery to resolve the token endpoint
            var discovery = await this.discoveryResolver.GetDiscovery(
                this.clientOptions.DiscoveryOptions,
                clientCredentialsOptions.OidcProviderAddress,
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
            var providerUri = new Uri(clientCredentialsOptions.OidcProviderAddress, UriKind.Absolute);
            var tokenEndpointUri = new Uri(providerUri, discovery.TokenEndpoint);
            // Request a token
            var httpClient = this.httpClientFactory.CreateClient(this.clientOptions.InternalHttpClientName);
            var form = new Dictionary<string, string>
            {
                { TokenEndpointConstant.GrantType, TokenEndpointConstant.ClientCredentials },
            };
            form.Add(TokenEndpointConstant.ClientId, clientCredentialsOptions.ClientId);
            form.Add(TokenEndpointConstant.ClientSecret, clientCredentialsOptions.ClientSecret);
            form.Add(TokenEndpointConstant.Scope, clientCredentialsOptions.Scope);
            var formContent = new FormUrlEncodedContent(form);
            var response = await httpClient.SendWithRetry(
                HttpMethod.Post,
                tokenEndpointUri,
                formContent,
                cancellationToken,
                this.clientOptions.MaxRetryAttempts,
                this.clientOptions.RetryDelayMilliseconds,
                this.logger
            );
            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogError(
                    "Unable to obtain token from {oidcProviderAddress} response {statusCode}",
                    clientCredentialsOptions.OidcProviderAddress,
                    response.StatusCode
                );
                return null;
            }
            var tokenResponse = await response.Content.ReadJsonAs<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                this.logger.LogError(
                    "No token returned from {oidcProviderAddress} for client {clientName}",
                    clientCredentialsOptions.OidcProviderAddress,
                    clientName
                );
                return null;
            }
            // Cache the value
            token = new ClientCredentialsToken(
                tokenResponse.AccessToken,
                this.utcNow.UtcNow.AddSeconds(tokenResponse.ExpiresIn ?? 0)
            );
            this.tokenDictionary.TryAdd(clientName, token);
            return token.AccessToken;
        }
    }
}
