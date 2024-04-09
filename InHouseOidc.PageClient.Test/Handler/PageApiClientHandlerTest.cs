// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.PageClient.Handler;
using InHouseOidc.PageClient.Resolver;
using InHouseOidc.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.PageClient.Test.Handler
{
    [TestClass]
    public class PageApiClientHandlerTest
    {
        private readonly TestMessageHandler testMessageHandler = new();
        private readonly string clientName = "TestClient";

        private Mock<IPageAccessTokenResolver> mockPageAccessTokenResolver = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.mockPageAccessTokenResolver = this.mockPageAccessTokenResolver = new(MockBehavior.Strict);
            this.testMessageHandler.Clear();
        }

        [TestMethod]
        public async Task PageApiClientHandler_SendAsync_Success()
        {
            // Arrange
            var pageApiClientHandler = new PageApiClientHandler(
                this.mockPageAccessTokenResolver.Object,
                this.clientName
            )
            {
                InnerHandler = this.testMessageHandler,
            };
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            this.mockPageAccessTokenResolver.Setup(m => m.GetClientToken(this.clientName, CancellationToken.None))
                .ReturnsAsync("token");
            var httpMessageInvoker = new HttpMessageInvoker(pageApiClientHandler);
            // Act
            var response = await httpMessageInvoker.SendAsync(httpRequestMessage, CancellationToken.None);
            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            Assert.AreEqual("bearer token", this.testMessageHandler.RequestMessage.Headers.Authorization?.ToString());
            this.mockPageAccessTokenResolver.VerifyAll();
        }
    }
}
