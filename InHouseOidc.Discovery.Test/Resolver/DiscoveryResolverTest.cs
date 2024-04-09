// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Net;
using InHouseOidc.Common;
using InHouseOidc.Common.Type;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Discovery.Test.Resolver
{
    [TestClass]
    public class DiscoveryResolverTest
    {
        private readonly TestLogger<DiscoveryResolver> logger = new();

        private DiscoveryOptions discoveryOptions = new();
        private Mock<IHttpClientFactory> mockHttpClientFactory = new();
        private Mock<IUtcNow> mockUtcNow = new();
        private DiscoveryResolver? discoveryResolver;
        private TestMessageHandler testMessageHandler = new();
        private DateTimeOffset utcNow = new DateTimeOffset(2022, 4, 23, 18, 22, 00, TimeSpan.Zero).ToUniversalTime();

        [TestInitialize]
        public void Initialise()
        {
            this.discoveryOptions = new();
            this.testMessageHandler = new TestMessageHandler();
            this.mockHttpClientFactory = new(MockBehavior.Strict);
            this.mockHttpClientFactory.Setup(m => m.CreateClient(this.discoveryOptions.InternalHttpClientName))
                .Returns(new HttpClient(this.testMessageHandler));
            this.mockUtcNow = new(MockBehavior.Strict);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
            this.discoveryResolver = new DiscoveryResolver(
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockUtcNow.Object
            );
        }

        [TestMethod]
        public async Task DiscoveryResolver_GetDiscovery_Success()
        {
            // Arrange
            var oidcProviderAddress = "https://localhost";
            var discoveryResponse = new DiscoveryResponse
            {
                AuthorizationEndpoint = "/authorization",
                EndSessionEndpoint = "/endsession",
                GrantTypesSupported = ["code "],
                Issuer = oidcProviderAddress,
                TokenEndpoint = "/token",
                TokenEndpointAuthMethodsSupported = ["client_secret_post"],
            };
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(discoveryResponse),
                StatusCode = HttpStatusCode.OK,
            };
            // Act 1 (not cached)
            Assert.IsNotNull(this.discoveryResolver);
            var discovery1 = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                oidcProviderAddress,
                CancellationToken.None
            );
            // Assert 1
            Assert.IsNotNull(discovery1);
            Assert.AreEqual(discoveryResponse.Issuer, discovery1.Issuer);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            // Act 2 (cached)
            var discovery2 = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                oidcProviderAddress,
                CancellationToken.None
            );
            Assert.IsNotNull(discovery2);
            Assert.AreEqual(discoveryResponse.Issuer, discovery2.Issuer);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            // Act 3 (cache expired)
            this.utcNow = this.utcNow.AddHours(1);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
            var discovery3 = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                oidcProviderAddress,
                CancellationToken.None
            );
            Assert.IsNotNull(discovery3);
            Assert.AreEqual(discoveryResponse.Issuer, discovery3.Issuer);
            Assert.AreEqual(2, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task DiscoveryResolver_GetDiscovery_EmptyDiscovery()
        {
            // Arrange
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(null),
                StatusCode = HttpStatusCode.OK,
            };
            // Act
            Assert.IsNotNull(this.discoveryResolver);
            var discovery = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert
            Assert.IsNull(discovery);
            this.logger.AssertLastItemContains(LogLevel.Error, "Unable to load discovery");
        }

        [TestMethod]
        public async Task DiscoveryResolver_GetDiscovery_InvalidGrantTypes()
        {
            // Arrange
            var discoveryResponse1 = new DiscoveryResponse();
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(discoveryResponse1),
                StatusCode = HttpStatusCode.OK,
            };
            var discoveryResponse2 = new DiscoveryResponse { GrantTypesSupported = [] };
            // Act 1
            Assert.IsNotNull(this.discoveryResolver);
            var discovery = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert 1
            Assert.IsNull(discovery);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid GrantTypesSupported");
            // Act 2
            this.testMessageHandler.ResponseMessage.Content = new TestJsonContent(discoveryResponse2);
            var discovery2 = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert 2
            Assert.IsNull(discovery2);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid GrantTypesSupported");
        }

        [TestMethod]
        public async Task DiscoveryResolver_GetDiscovery_InvalidIssuer()
        {
            // Arrange
            var discoveryResponse1 = new DiscoveryResponse { GrantTypesSupported = ["code"] };
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(discoveryResponse1),
                StatusCode = HttpStatusCode.OK,
            };
            var discoveryResponse2 = new DiscoveryResponse
            {
                GrantTypesSupported = ["code"],
                Issuer = "https://otherissuer",
            };
            // Act 1
            Assert.IsNotNull(this.discoveryResolver);
            var discovery1 = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert 1
            Assert.IsNull(discovery1);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid Issuer");
            // Act 2
            this.testMessageHandler.ResponseMessage.Content = new TestJsonContent(discoveryResponse2);
            var discovery2 = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert 2
            Assert.IsNull(discovery2);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid Issuer");
        }

        [TestMethod]
        public async Task DiscoveryResolver_GetDiscovery_InvalidTokenEndpointAuthMethodsSupported()
        {
            // Arrange
            var discoveryResponse = new DiscoveryResponse
            {
                GrantTypesSupported = ["code "],
                Issuer = "https://localhost",
                TokenEndpoint = "/token",
            };
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(discoveryResponse),
                StatusCode = HttpStatusCode.OK,
            };
            // Act 1
            Assert.IsNotNull(this.discoveryResolver);
            var discovery = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert 1
            Assert.IsNull(discovery);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid TokenEndpointAuthMethodsSupported");
            // Act 2
            discoveryResponse.TokenEndpointAuthMethodsSupported = ["client_secret_post"];
            var discovery2 = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert 2
            Assert.IsNull(discovery2);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid TokenEndpointAuthMethodsSupported");
        }

        [TestMethod]
        public async Task DiscoveryResolver_GetDiscovery_InvalidDiscoveryIgnored()
        {
            // Arrange
            var discoveryResponse = new DiscoveryResponse
            {
                GrantTypesSupported = null,
                Issuer = null,
                TokenEndpoint = "/token",
                TokenEndpointAuthMethodsSupported = ["client_secret_post"],
            };
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(discoveryResponse),
                StatusCode = HttpStatusCode.OK,
            };
            this.discoveryOptions.ValidateIssuer = false;
            this.discoveryOptions.ValidateGrantTypes = false;
            // Act
            Assert.IsNotNull(this.discoveryResolver);
            var discovery = await this.discoveryResolver.GetDiscovery(
                this.discoveryOptions,
                "https://localhost",
                CancellationToken.None
            );
            // Assert
            Assert.IsNotNull(discovery);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
        }
    }
}
