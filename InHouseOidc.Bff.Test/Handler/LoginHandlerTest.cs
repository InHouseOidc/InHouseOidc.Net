// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Bff.Resolver;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace InHouseOidc.Bff.Test.Handler
{
    [TestClass]
    public class LoginHandlerTest
    {
        private readonly BffClientOptions bffClientOptions =
            new()
            {
                ClientId = "bffclientid",
                ClientSecret = "topsecret",
                OidcProviderAddress = "https://localhost",
                Scope = "bffscope",
            };
        private readonly Mock<IBffClientResolver> mockBffClientResolver = new(MockBehavior.Strict);
        private readonly Mock<ILogger> mockLogger = new();
        private readonly Mock<ILoggerFactory> mockLoggerFactory = new(MockBehavior.Strict);

        private Mock<IAuthenticationService> mockAuthenticationService = new(MockBehavior.Strict);
        private HttpContext context = new DefaultHttpContext();

        [TestInitialize]
        public void TestInitialise()
        {
            this.context = new DefaultHttpContext();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(p => p.GetService(typeof(ILoggerFactory))).Returns(this.mockLoggerFactory.Object);
            this.mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(this.mockLogger.Object);
            this.mockAuthenticationService = new Mock<IAuthenticationService>(MockBehavior.Strict);
            this.mockAuthenticationService.Setup(m =>
                    m.ChallengeAsync(
                        this.context,
                        BffConstant.AuthenticationSchemeBff,
                        It.IsAny<AuthenticationProperties>()
                    )
                )
                .Returns(Task.CompletedTask);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(this.mockAuthenticationService.Object);
            this.context.RequestServices = mockServiceProvider.Object;
            this.mockBffClientResolver.Setup(m => m.GetClient(It.IsAny<HttpContext>()))
                .Returns((this.bffClientOptions, BffConstant.AuthenticationSchemeBff));
        }

        [TestMethod]
        public async Task HandleRequest_Challenge_Success()
        {
            // Arrange
            const string redirectUri = "/return";
            this.context.Request.Method = "GET";
            this.context.Request.QueryString = new QueryString($"?returnUrl={redirectUri}");
            var loginHandler = new LoginHandler(this.mockBffClientResolver.Object);
            // Act
            var result = await loginHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            this.mockAuthenticationService.Verify(
                m =>
                    m.ChallengeAsync(
                        It.Is<HttpContext>(c => c == this.context),
                        It.Is<string>(s => s == BffConstant.AuthenticationSchemeBff),
                        It.Is<AuthenticationProperties>(p => p.RedirectUri == redirectUri)
                    ),
                Times.Once()
            );
        }

        [TestMethod]
        public async Task HandleRequest_MethodNotAllowed()
        {
            // Arrange
            this.context.Request.Method = "POST";
            var loginHandler = new LoginHandler(this.mockBffClientResolver.Object);
            // Act
            var result = await loginHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(405, this.context.Response.StatusCode);
        }
    }
}
