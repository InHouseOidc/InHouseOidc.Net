// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.PageClient.Resolver;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.PageClient.Test
{
    [TestClass]
    public class AddPageApiClientTokenExtensionTest
    {
        [TestMethod]
        public void AddPageApiClientToken_MessageHandler()
        {
            // Arrange
            var clientName = "TestClient";
            var serviceCollection = new TestServiceCollection();
            var pageAccessTokenResolver = new Mock<IPageAccessTokenResolver>();
            serviceCollection.AddSingleton(pageAccessTokenResolver.Object);
            // Act
            serviceCollection.AddHttpClient(clientName).AddPageApiClientToken(clientName);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Assert
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            Assert.IsNotNull(httpClient);
        }
    }
}
