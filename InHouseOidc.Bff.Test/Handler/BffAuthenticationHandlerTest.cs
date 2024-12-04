// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Bff.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace InHouseOidc.Bff.Test.Handler
{
    [TestClass]
    public class BffAuthenticationHandlerTest
    {
        private readonly ClientOptions clientOptions = new();
        private readonly TestLogger<BffAuthenticationHandler> logger = new();
        private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> mockIOptionsMonitor =
            new(MockBehavior.Strict);

        private readonly AuthenticationScheme authenticationScheme =
            new(BffConstant.AuthenticationSchemeBff, null, typeof(BffAuthenticationHandler));
        private HttpContext context = new DefaultHttpContext();
        private TestServiceCollection serviceCollection = [];
        private Mock<ILoggerFactory> mockLoggerFactory = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.context = new DefaultHttpContext();
            this.mockLoggerFactory = new(MockBehavior.Strict);
            this.mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(this.logger);
            this.mockIOptionsMonitor.Setup(m => m.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());
            var mockIOptionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>(MockBehavior.Strict);
            this.logger.Clear();
            this.serviceCollection = [];
        }

        [DataTestMethod]
        [DataRow("{null}")]
        [DataRow("")]
        [DataRow("/")]
        [DataRow("/ignored")]
        public async Task HandleRequestAsync_IgnoredPath(string? pathString)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var bffAuthenticationHandler = new BffAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.clientOptions,
                this.mockLoggerFactory.Object,
                this.serviceCollection.BuildServiceProvider(),
                UrlEncoder.Default
            );
            await bffAuthenticationHandler.InitializeAsync(this.authenticationScheme, this.context);
            this.context.Request.Path = pathString == "{null}" ? null : new PathString(pathString);
            // Act
            var result = await bffAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task HandleRequestAsync_Login_Success()
        {
            // Arrange
            var mockEndpointHandler = new Mock<IEndpointHandler<LoginHandler>>();
            mockEndpointHandler.Setup(m => m.HandleRequest(It.IsAny<HttpContext>())).ReturnsAsync(true);
            this.serviceCollection.AddScoped(
                typeof(IEndpointHandler<LoginHandler>),
                (serviceProvider) => mockEndpointHandler.Object
            );
            var bffAuthenticationHandler = new BffAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.clientOptions,
                this.mockLoggerFactory.Object,
                this.serviceCollection.BuildServiceProvider(),
                UrlEncoder.Default
            );
            await bffAuthenticationHandler.InitializeAsync(this.authenticationScheme, this.context);
            this.context.Request.Path = new PathString("/api/auth/login");
            // Act
            var result = await bffAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task HandleAuthenticateAsync_NotImplemented()
        {
            // Arrange
            var bffAuthenticationHandler = new BffAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.clientOptions,
                this.mockLoggerFactory.Object,
                this.serviceCollection.BuildServiceProvider(),
                UrlEncoder.Default
            );
            await bffAuthenticationHandler.InitializeAsync(this.authenticationScheme, this.context);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<NotImplementedException>(
                async () => await bffAuthenticationHandler.AuthenticateAsync()
            );
            // Assert
            Assert.IsNotNull(exception);
        }
    }
}
