// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Bff.Resolver;
using InHouseOidc.Test.Common;
using Moq;

namespace InHouseOidc.Bff.Test.Handler
{
    [TestClass]
    public class BffApiClientHandlerTest
    {
        private readonly TestMessageHandler testMessageHandler = new();
        private readonly string clientName = "TestClient";

        private Mock<IBffAccessTokenResolver> mockBffAccessTokenResolver = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.mockBffAccessTokenResolver = this.mockBffAccessTokenResolver = new(MockBehavior.Strict);
            this.testMessageHandler.Clear();
        }

        [TestMethod]
        public async Task BffApiClientHandler_SendAsync_Success()
        {
            // Arrange
            var pageApiClientHandler = new BffApiClientHandler(this.mockBffAccessTokenResolver.Object, this.clientName)
            {
                InnerHandler = this.testMessageHandler,
            };
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            this.mockBffAccessTokenResolver.Setup(m => m.GetClientToken(this.clientName, CancellationToken.None))
                .ReturnsAsync("token");
            var httpMessageInvoker = new HttpMessageInvoker(pageApiClientHandler);
            // Act
            var response = await httpMessageInvoker.SendAsync(httpRequestMessage, CancellationToken.None);
            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            Assert.AreEqual("bearer token", this.testMessageHandler.RequestMessage.Headers.Authorization?.ToString());
            this.mockBffAccessTokenResolver.VerifyAll();
        }
    }
}
