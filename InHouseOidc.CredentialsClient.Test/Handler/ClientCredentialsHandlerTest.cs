// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.CredentialsClient.Handler;
using InHouseOidc.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.CredentialsClient.Test.Handler
{
    [TestClass]
    public class ClientCredentialsHandlerTest
    {
        private readonly TestMessageHandler testMessageHandler = new();
        private readonly string clientName = "TestClient";

        private Mock<IClientCredentialsResolver> mockClientCredentialsResolver = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.mockClientCredentialsResolver = this.mockClientCredentialsResolver = new(MockBehavior.Strict);
            this.testMessageHandler.Clear();
        }

        [TestMethod]
        public async Task ClientCredentialsHandler_SendAsync_Success()
        {
            // Arrange
            var clientCredentialsHandler = new ClientCredentialsHandler(
                this.mockClientCredentialsResolver.Object,
                this.clientName
            )
            {
                InnerHandler = this.testMessageHandler,
            };
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            this.mockClientCredentialsResolver.Setup(m => m.GetClientToken(this.clientName, CancellationToken.None))
                .ReturnsAsync("token");
            var httpMessageInvoker = new HttpMessageInvoker(clientCredentialsHandler);
            // Act
            var response = await httpMessageInvoker.SendAsync(httpRequestMessage, CancellationToken.None);
            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            Assert.AreEqual("bearer token", this.testMessageHandler.RequestMessage.Headers.Authorization?.ToString());
            this.mockClientCredentialsResolver.VerifyAll();
        }

        [TestMethod]
        public async Task ClientCredentialsHandler_SendAsync_Fail401()
        {
            // Arrange
            var clientCredentialsHandler = new ClientCredentialsHandler(
                this.mockClientCredentialsResolver.Object,
                this.clientName
            )
            {
                InnerHandler = this.testMessageHandler,
            };
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "http://localhost");
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
            this.mockClientCredentialsResolver.Setup(m => m.GetClientToken(this.clientName, CancellationToken.None))
                .ReturnsAsync("token");
            this.mockClientCredentialsResolver.Setup(m => m.ClearClientToken(this.clientName))
                .Returns(Task.CompletedTask);
            var httpMessageInvoker = new HttpMessageInvoker(clientCredentialsHandler);
            // Act
            var response = await httpMessageInvoker.SendAsync(httpRequestMessage, CancellationToken.None);
            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(2, this.testMessageHandler.SendCount);
            Assert.AreEqual("bearer token", this.testMessageHandler.RequestMessage.Headers.Authorization?.ToString());
            this.mockClientCredentialsResolver.VerifyAll();
        }
    }
}
