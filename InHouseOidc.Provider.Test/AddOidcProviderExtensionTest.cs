// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Handler;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Provider.Test
{
    [TestClass]
    public class AddOidcProviderExtensionTest
    {
        [TestMethod]
        public void AddOidcProvider()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var mockClientStore = new Mock<IClientStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockClientStore.Object);
            var mockCodeStore = new Mock<ICodeStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockCodeStore.Object);
            var mockResourceStore = new Mock<IResourceStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockResourceStore.Object);
            // Act
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(System.DateTimeOffset.UtcNow) });
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _ = serviceProvider.GetRequiredService<IEndpointHandler<DiscoveryHandler>>();
            _ = serviceProvider.GetRequiredService<IEndpointHandler<JsonWebKeySetHandler>>();
            _ = serviceProvider.GetRequiredService<IJsonWebTokenHandler>();
            _ = serviceProvider.GetRequiredService<ProviderAuthenticationHandler>();
            _ = serviceProvider.GetRequiredService<IEndpointHandler<TokenHandler>>();
            _ = serviceProvider.GetRequiredService<IValidationHandler>();
        }
    }
}
