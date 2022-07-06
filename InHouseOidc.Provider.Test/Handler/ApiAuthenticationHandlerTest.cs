// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class ApiAuthenticationHandlerTest
    {
        private const string Audience = "aud";

        private readonly TestLogger<ApiAuthenticationHandler> logger = new();
        private readonly Mock<ILoggerFactory> mockLoggerFactory = new();
        private readonly Mock<ISystemClock> mockSystemClock = new(MockBehavior.Strict);
        private readonly Mock<IValidationHandler> mockValidationHandler = new(MockBehavior.Strict);
        private readonly ApiAuthenticationOptions apiAuthenticationOptions = new() { Audience = Audience };

        private ApiAuthenticationHandler? apiAuthenticationHandler;

        [TestInitialize]
        public void Initialise()
        {
            var mockIOptionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>(MockBehavior.Strict);
            mockIOptionsMonitor.Setup(m => m.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());
            this.mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(this.logger);
            this.logger.Clear();
            this.apiAuthenticationHandler = new ApiAuthenticationHandler(
                this.apiAuthenticationOptions,
                mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.mockSystemClock.Object,
                UrlEncoder.Default,
                this.mockValidationHandler.Object
            );
        }

        [DataTestMethod]
        [DataRow("none")]
        [DataRow("")]
        [DataRow("Bear")]
        [DataRow("Bearer ")]
        [DataRow("Bearer    ")]
        public async Task HandleAuthenticateAsync_NoHeader(string headerValue)
        {
            // Arrange
            var context = new DefaultHttpContext();
            Assert.IsNotNull(this.apiAuthenticationHandler);
            var authenticationScheme = new AuthenticationScheme(
                ApiConstant.AuthenticationScheme,
                null,
                typeof(ApiAuthenticationHandler)
            );
            await this.apiAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            if (headerValue != "none")
            {
                context.Request.Headers.Authorization = headerValue;
            }
            // Act
            var result = await this.apiAuthenticationHandler.AuthenticateAsync();
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.None);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task HandleAuthenticateAsync_TokenValidation(bool isValidToken)
        {
            // Arrange
            var context = new DefaultHttpContext();
            Assert.IsNotNull(this.apiAuthenticationHandler);
            var authenticationScheme = new AuthenticationScheme(
                ApiConstant.AuthenticationScheme,
                null,
                typeof(ApiAuthenticationHandler)
            );
            await this.apiAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            var token = "token";
            var issuer = "localhost";
            context.Request.Headers.Authorization = ApiConstant.Bearer + token;
            context.Request.Headers.Host = issuer;
            context.Request.Scheme = "https";
            var validationResult = isValidToken
                ? new ClaimsPrincipal(new ClaimsIdentity(ApiConstant.AuthenticationScheme))
                : null;
            this.mockValidationHandler
                .Setup(m => m.ValidateJsonWebToken(Audience, $"https://{issuer}", token, true))
                .Returns(validationResult);
            // Act
            var result = await this.apiAuthenticationHandler.AuthenticateAsync();
            // Assert
            Assert.IsNotNull(result);
            if (isValidToken)
            {
                Assert.IsTrue(result.Succeeded);
            }
            else
            {
                Assert.IsTrue(result.None);
            }
        }
    }
}
