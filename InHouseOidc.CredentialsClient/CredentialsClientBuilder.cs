// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.CredentialsClient.Resolver;
using InHouseOidc.CredentialsClient.Type;
using InHouseOidc.Discovery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InHouseOidc.CredentialsClient
{
    /// <summary>
    /// Builds the services required to support client access to APIs secured with OIDC Provider.
    /// </summary>
    public class CredentialsClientBuilder
    {
        internal ClientOptions ClientOptions { get; } = new ClientOptions();
        internal IServiceCollection ServiceCollection { get; set; }

        public CredentialsClientBuilder(IServiceCollection serviceCollection)
        {
            this.ServiceCollection = serviceCollection;
        }

        /// <summary>
        /// Sets up a named HttpClient with client credentials token access.
        /// </summary>
        /// <param name="clientName">The HttpClient name to allocate.</param>
        /// <param name="credentialsClientOptions">The options used to obtain a client credentials access token.</param>
        /// <returns><see cref="CredentialsClientBuilder"/> so additional calls can be chained.</returns>
        public CredentialsClientBuilder AddClient(string clientName, CredentialsClientOptions credentialsClientOptions)
        {
            // Configure
            if (!this.ClientOptions.CredentialsClientsOptions.TryAdd(clientName, credentialsClientOptions))
            {
                throw new ArgumentException($"Duplicate client name: {clientName}", nameof(clientName));
            }
            return this;
        }

        /// <summary>
        /// Builds the final services for the client. Required as the final step of the client setup.
        /// </summary>
        public void Build()
        {
            this.ClientOptions.DiscoveryOptions.DiscoveryCacheTime = this.ClientOptions.DiscoveryCacheTime;
            this.ClientOptions.DiscoveryOptions.InternalHttpClientName = this.ClientOptions.InternalHttpClientName;
            this.ClientOptions.DiscoveryOptions.MaxRetryAttempts = this.ClientOptions.MaxRetryAttempts;
            this.ClientOptions.DiscoveryOptions.RetryDelayMilliseconds = this.ClientOptions.RetryDelayMilliseconds;
            this.ServiceCollection.AddSingleton(this.ClientOptions);
            this.ServiceCollection.AddHttpClient(this.ClientOptions.InternalHttpClientName);
            this.ServiceCollection.TryAddSingleton<IDiscoveryResolver, DiscoveryResolver>();
            this.ServiceCollection.TryAddSingleton<IClientCredentialsResolver, ClientCredentialsResolver>();
            // Setup the credentials clients added
            if (this.ClientOptions.CredentialsClientsOptions.Any())
            {
                var clientNames = this.ClientOptions.CredentialsClientsOptions.Keys;
                foreach (var clientName in clientNames)
                {
                    // Add the HTTP client and bind the token handler
                    this.ServiceCollection.AddHttpClient(clientName).AddClientCredentialsToken();
                }
            }
        }

        /// <summary>
        /// Sets time to cache discovery information.  Optional (defaults to 30 minutes).
        /// </summary>
        /// <param name="discoveryCacheTime">The TimeSpan to cache for.</param>
        /// <returns><see cref="CredentialsClientBuilder"/> so additional calls can be chained.</returns>
        public CredentialsClientBuilder SetDiscoveryCacheTime(TimeSpan discoveryCacheTime)
        {
            this.ClientOptions.DiscoveryCacheTime = discoveryCacheTime;
            return this;
        }

        /// <summary>
        /// Sets HttpClient name to use for internal operations.  Optional (defaults to "InHouseOidc.HttpClient").
        /// </summary>
        /// <param name="internalHttpClientName">The HttpClient name.</param>
        /// <returns><see cref="CredentialsClientBuilder"/> so additional calls can be chained.</returns>
        public CredentialsClientBuilder SetInternalHttpClientName(string internalHttpClientName)
        {
            this.ClientOptions.InternalHttpClientName = internalHttpClientName;
            this.ServiceCollection.AddHttpClient(internalHttpClientName);
            return this;
        }

        /// <summary>
        /// Sets the maximum retry attempts to make when making provider requests.  Optional (defaults to 5).
        /// </summary>
        /// <param name="maxRetryAttempts">The maximum retry attempts.</param>
        /// <returns><see cref="CredentialsClientBuilder"/> so additional calls can be chained.</returns>
        public CredentialsClientBuilder SetMaxRetryAttempts(int maxRetryAttempts)
        {
            this.ClientOptions.MaxRetryAttempts = maxRetryAttempts;
            return this;
        }

        /// <summary>
        /// Sets the base delay time to use between retryable provider requests.  Optional (defaults to 50 milliseconds).<br />
        /// Each progressive retry doubles the delay time between attempts, e.g. 50ms, 100ms, 200ms, 400ms, 800ms.
        /// </summary>
        /// <param name="retryDelayMilliseconds">The retry base time in milliseconds.</param>
        /// <returns><see cref="CredentialsClientBuilder"/> so additional calls can be chained.</returns>
        public CredentialsClientBuilder SetRetryDelayMilliseconds(int retryDelayMilliseconds)
        {
            this.ClientOptions.RetryDelayMilliseconds = retryDelayMilliseconds;
            return this;
        }
    }
}
