// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.CredentialsClient.Test
{
    [TestClass]
    public class AddClientCredentialsTokenExtensionTest
    {
        [TestMethod]
        public void AddClientCredentialsToken_MessageHandler()
        {
            // Arrange
            var clientName = "TestClient";
            var serviceCollection = new TestServiceCollection();
            var clientCredentialsResolver = new Mock<IClientCredentialsResolver>();
            serviceCollection.AddSingleton(clientCredentialsResolver.Object);
            // Act
            serviceCollection.AddHttpClient(clientName).AddClientCredentialsToken();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Assert
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            Assert.IsNotNull(httpClient);
        }
    }
}
