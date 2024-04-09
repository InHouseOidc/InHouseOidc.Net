// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Extension;
using InHouseOidc.Common.Type;
using Microsoft.Extensions.Logging;

namespace InHouseOidc.Discovery
{
    public class DiscoveryResolver(
        IHttpClientFactory httpClientFactory,
        ILogger<DiscoveryResolver> logger,
        IUtcNow utcNow
    ) : IDiscoveryResolver
    {
        private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
        private readonly ILogger<DiscoveryResolver> logger = logger;
        private readonly IUtcNow utcNow = utcNow;
        private readonly ConcurrentDictionary<string, Discovery> discoveryDictionary = new();

        public async Task<Discovery?> GetDiscovery(
            DiscoveryOptions discoveryOptions,
            string oidcProviderAddress,
            CancellationToken cancellationToken
        )
        {
            // Check for cached value available
            if (this.discoveryDictionary.TryGetValue(oidcProviderAddress, out var discovery))
            {
                if (discovery.ExpiryUtc > this.utcNow.UtcNow)
                {
                    return discovery;
                }
            }
            // Access the discovery information from the provider
            var httpClient = this.httpClientFactory.CreateClient(discoveryOptions.InternalHttpClientName);
            var providerUri = new Uri(oidcProviderAddress.EnsureEndsWithSlash(), UriKind.Absolute);
            var discoveryUri = new Uri(providerUri, ".well-known/openid-configuration");
            var response = await httpClient.SendWithRetry(
                HttpMethod.Get,
                discoveryUri,
                null,
                cancellationToken,
                discoveryOptions.MaxRetryAttempts,
                discoveryOptions.RetryDelayMilliseconds,
                this.logger
            );
            response.EnsureSuccessStatusCode();
            var discoveryResponse = await response.Content.ReadJsonAs<DiscoveryResponse>();
            // Validate the discovery response
            if (discoveryResponse == null)
            {
                this.logger.LogError("Unable to load discovery from {oidcProviderAddress}", oidcProviderAddress);
                return null;
            }
            if (
                discoveryOptions.ValidateGrantTypes
                && (discoveryResponse.GrantTypesSupported == null || discoveryResponse.GrantTypesSupported.Count == 0)
            )
            {
                this.logger.LogError(
                    "Invalid GrantTypesSupported response from {oidcProviderAddress}",
                    oidcProviderAddress
                );
                return null;
            }
            if (
                discoveryOptions.ValidateIssuer
                && (string.IsNullOrEmpty(discoveryResponse.Issuer) || discoveryResponse.Issuer != oidcProviderAddress)
            )
            {
                this.logger.LogError("Invalid Issuer response from {oidcProviderAddress}", oidcProviderAddress);
                return null;
            }
            if (
                discoveryResponse.TokenEndpointAuthMethodsSupported == null
                || discoveryResponse.TokenEndpointAuthMethodsSupported.Count == 0
            )
            {
                this.logger.LogError(
                    "Invalid TokenEndpointAuthMethodsSupported response from {oidcProviderAddress}",
                    oidcProviderAddress
                );
                return null;
            }
            // Cache the value and return it
            discovery = new Discovery(
                discoveryResponse.AuthorizationEndpoint,
                discoveryResponse.EndSessionEndpoint,
                this.utcNow.UtcNow.Add(discoveryOptions.CacheTime),
                discoveryResponse.GrantTypesSupported ?? [],
                discoveryResponse.Issuer ?? string.Empty,
                discoveryResponse.TokenEndpoint,
                discoveryResponse.TokenEndpointAuthMethodsSupported
            );
            this.discoveryDictionary[oidcProviderAddress] = discovery;
            return discovery;
        }
    }
}
