// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Bff.Resolver;
using InHouseOidc.Bff.Type;
using InHouseOidc.Common.Constant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace InHouseOidc.Bff.Test.Handler
{
    [TestClass]
    public class LogoutHandlerTest
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
        private readonly ClientOptions clientOptions = new();
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
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(this.mockAuthenticationService.Object);
            this.context.RequestServices = mockServiceProvider.Object;
            this.mockBffClientResolver.Setup(m => m.GetClient(It.IsAny<HttpContext>()))
                .Returns((this.bffClientOptions, BffConstant.AuthenticationSchemeBff));
        }

        [TestMethod]
        public async Task HandleRequest_NotLoggedIn()
        {
            // Arrange
            var authenticateResult = AuthenticateResult.NoResult();
            this.mockAuthenticationService.Setup(m =>
                    m.AuthenticateAsync(this.context, BffConstant.AuthenticationSchemeCookie)
                )
                .ReturnsAsync(authenticateResult);
            this.context.Request.Method = "GET";
            var logoutHandler = new LogoutHandler(this.mockBffClientResolver.Object, this.clientOptions);
            // Act
            var result = await logoutHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(302, this.context.Response.StatusCode);
            Assert.AreEqual(this.clientOptions.PostLogoutRedirectAddress, this.context.Response.Headers.Location);
        }

        [TestMethod]
        public async Task HandleRequest_Logout_Success()
        {
            // Arrange
            var authenticationProperties = new AuthenticationProperties();
            var sessionId = "session.id";
            var claims = new List<Claim> { new(JsonWebTokenClaim.SessionId, sessionId) };
            var claimsIdentity = new ClaimsIdentity(claims, BffConstant.AuthenticationSchemeCookie);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var ticket = new AuthenticationTicket(
                claimsPrincipal,
                authenticationProperties,
                BffConstant.AuthenticationSchemeCookie
            );
            var authenticateResult = AuthenticateResult.Success(ticket);
            this.mockAuthenticationService.Setup(m =>
                    m.AuthenticateAsync(this.context, BffConstant.AuthenticationSchemeCookie)
                )
                .ReturnsAsync(authenticateResult);
            this.mockAuthenticationService.Setup(m =>
                    m.SignOutAsync(this.context, BffConstant.AuthenticationSchemeCookie, null)
                )
                .Returns(Task.CompletedTask);
            this.mockAuthenticationService.Setup(m =>
                    m.SignOutAsync(
                        this.context,
                        BffConstant.AuthenticationSchemeBff,
                        It.IsAny<AuthenticationProperties>()
                    )
                )
                .Returns(Task.CompletedTask);
            this.context.Request.Method = "GET";
            this.context.Request.QueryString = new QueryString($"?sessionId={sessionId}");
            var logoutHandler = new LogoutHandler(this.mockBffClientResolver.Object, this.clientOptions);
            // Act
            var result = await logoutHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            this.mockAuthenticationService.Verify(
                m =>
                    m.SignOutAsync(
                        It.Is<HttpContext>(c => c == this.context),
                        It.Is<string>(s => s == BffConstant.AuthenticationSchemeCookie),
                        It.IsAny<AuthenticationProperties>()
                    ),
                Times.Once()
            );
            this.mockAuthenticationService.Verify(
                m =>
                    m.SignOutAsync(
                        It.Is<HttpContext>(c => c == this.context),
                        It.Is<string>(s => s == BffConstant.AuthenticationSchemeBff),
                        It.Is<AuthenticationProperties>(p =>
                            p.RedirectUri == this.clientOptions.PostLogoutRedirectAddress
                        )
                    ),
                Times.Once()
            );
        }

        [TestMethod]
        public async Task HandleRequest_NoSessionId()
        {
            // Arrange
            var authenticationProperties = new AuthenticationProperties();
            var claimsIdentity = new ClaimsIdentity(BffConstant.AuthenticationSchemeCookie);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var ticket = new AuthenticationTicket(
                claimsPrincipal,
                authenticationProperties,
                BffConstant.AuthenticationSchemeCookie
            );
            var authenticateResult = AuthenticateResult.Success(ticket);
            this.mockAuthenticationService.Setup(m =>
                    m.AuthenticateAsync(this.context, BffConstant.AuthenticationSchemeCookie)
                )
                .ReturnsAsync(authenticateResult);
            this.context.Request.Method = "GET";
            var logoutHandler = new LogoutHandler(this.mockBffClientResolver.Object, this.clientOptions);
            // Act
            var result = await logoutHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(400, this.context.Response.StatusCode);
        }

        [TestMethod]
        public async Task HandleRequest_MethodNotAllowed()
        {
            // Arrange
            this.context.Request.Method = "POST";
            var logoutHandler = new LogoutHandler(this.mockBffClientResolver.Object, this.clientOptions);
            // Act
            var result = await logoutHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(405, this.context.Response.StatusCode);
        }
    }
}
