// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.CredentialsClient.Type;
using InHouseOidc.Discovery;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.CredentialsClient.Test
{
    [TestClass]
    public class CredentialsClientBuilderTest
    {
        private TestServiceCollection serviceCollection = [];

        [TestInitialize]
        public void Initialise()
        {
            this.serviceCollection = [];
        }

        [TestMethod]
        public void ClientBuilder_AddClient()
        {
            // Arrange
            var clientName = "CredentialsClient";
            var clientOptions = new CredentialsClientOptions
            {
                ClientId = clientName,
                ClientSecret = "TopSecret",
                OidcProviderAddress = "https://localhost",
                Scope = "scope1",
            };
            // Act
            var clientBuilder = this.serviceCollection.AddOidcCredentialsClient().AddClient(clientName, clientOptions);
            var duplicateException = Assert.ThrowsException<ArgumentException>(
                () => clientBuilder.AddClient(clientName, clientOptions)
            );
            clientBuilder.Build();
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            // Assert
            _ = serviceProvider.GetRequiredService<ClientOptions>();
            _ = serviceProvider.GetRequiredService<IDiscoveryResolver>();
            _ = serviceProvider.GetRequiredService<IClientCredentialsResolver>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            Assert.IsNotNull(httpClient);
            Assert.IsNotNull(duplicateException);
            Assert.IsTrue(duplicateException.Message.Contains("Duplicate client name"));
        }

        [TestMethod]
        public void ClientBuilder_SetAll()
        {
            // Arrange
            var discoveryCacheTime = TimeSpan.FromHours(1);
            var internalHttpClientName = "InHouseOidc";
            var maxRetryAttempts = 10;
            var retryDelayMilliseconds = 25;
            // Act
            var clientBuilder = this
                .serviceCollection.AddOidcCredentialsClient()
                .EnableDiscoveryGrantTypeValidation(false)
                .EnableDiscoveryIssuerValidation(false)
                .SetDiscoveryCacheTime(discoveryCacheTime)
                .SetInternalHttpClientName(internalHttpClientName)
                .SetMaxRetryAttempts(maxRetryAttempts)
                .SetRetryDelayMilliseconds(retryDelayMilliseconds);
            clientBuilder.Build();
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            // Assert
            var clientOptions = serviceProvider.GetRequiredService<ClientOptions>();
            Assert.AreEqual(discoveryCacheTime, clientOptions.DiscoveryOptions.CacheTime);
            Assert.AreEqual(internalHttpClientName, clientOptions.InternalHttpClientName);
            Assert.AreEqual(maxRetryAttempts, clientOptions.MaxRetryAttempts);
            Assert.AreEqual(retryDelayMilliseconds, clientOptions.RetryDelayMilliseconds);
            Assert.IsFalse(clientOptions.DiscoveryOptions.ValidateGrantTypes);
            Assert.IsFalse(clientOptions.DiscoveryOptions.ValidateIssuer);
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(internalHttpClientName);
            Assert.IsNotNull(httpClient);
        }
    }
}
