// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Bff.Resolver;
using InHouseOidc.Bff.Type;
using InHouseOidc.Common.Constant;
using InHouseOidc.Discovery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;

namespace InHouseOidc.Bff.Test.Handler
{
    [TestClass]
    public class UserInfoHandlerTest
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
        private readonly Mock<IDiscoveryResolver> mockDiscoveryResolver = new(MockBehavior.Strict);
        private readonly Mock<ILogger> mockLogger = new();
        private readonly Mock<ILoggerFactory> mockLoggerFactory = new(MockBehavior.Strict);

        private Mock<IAuthenticationService> mockAuthenticationService = new(MockBehavior.Strict);
        private HttpContext context = new DefaultHttpContext();
        private Discovery.Discovery? discovery;

        [TestInitialize]
        public void TestInitialise()
        {
            this.context = new DefaultHttpContext();
            this.context.Response.Body = new MemoryStream();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            mockServiceProvider.Setup(p => p.GetService(typeof(ILoggerFactory))).Returns(this.mockLoggerFactory.Object);
            this.mockLoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(this.mockLogger.Object);
            var mockJsonOptions = new Mock<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>();
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>)))
                .Returns(mockJsonOptions.Object);
            this.mockAuthenticationService = new Mock<IAuthenticationService>(MockBehavior.Strict);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(this.mockAuthenticationService.Object);
            this.context.RequestServices = mockServiceProvider.Object;
            this.mockBffClientResolver.Setup(m => m.GetClient(It.IsAny<HttpContext>()))
                .Returns((this.bffClientOptions, BffConstant.AuthenticationSchemeBff));
            this.discovery = new Discovery.Discovery(
                null,
                "https://localhost/checksession",
                null,
                DateTimeOffset.MaxValue,
                ["code"],
                this.bffClientOptions.OidcProviderAddress,
                "/token",
                [DiscoveryConstant.ClientSecretPost]
            );
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.bffClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(this.discovery);
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
            var userInfoHandler = new UserInfoHandler(
                this.mockBffClientResolver.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object
            );
            // Act
            var result = await userInfoHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, this.context.Response.StatusCode);
            Assert.AreEqual("{\"isAuthenticated\":false}", this.GetResponseBody());
        }

        [TestMethod]
        public async Task HandleRequest_MethodNotAllowed()
        {
            // Arrange
            this.context.Request.Method = "POST";
            var userInfoHandler = new UserInfoHandler(
                this.mockBffClientResolver.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object
            );
            // Act
            var result = await userInfoHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(405, this.context.Response.StatusCode);
        }

        [DataTestMethod]
        [DataRow(false, false, false)]
        [DataRow(true, true, true)]
        [DataRow(true, false, false)]
        public async Task HandleRequest_Success(bool setProperties, bool setExpiresUtc, bool setSessionState)
        {
            // Arrange
            DateTimeOffset? expiresUtc = setExpiresUtc ? DateTimeOffset.UtcNow + TimeSpan.FromHours(1) : null;
            var sessionState = setSessionState ? "session.state" : null;
            var items = new Dictionary<string, string?>();
            if (setSessionState)
            {
                items.Add(OpenIdConnectSessionProperties.SessionState, sessionState);
            }
            var authenticationProperties = setProperties
                ? new AuthenticationProperties(items) { ExpiresUtc = expiresUtc, }
                : null;
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
            this.context.Request.Method = "GET";
            var userInfoHandler = new UserInfoHandler(
                this.mockBffClientResolver.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object
            );
            // Act
            var result = await userInfoHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, this.context.Response.StatusCode);
            var expectedBody = new
            {
                checkSessionUri = this.discovery?.CheckSessionEndpoint,
                claims = new[] { new { type = JsonWebTokenClaim.SessionId, value = sessionId } },
                clientId = this.bffClientOptions.ClientId,
                isAuthenticated = true,
                sessionExpiry = expiresUtc?.ToString("u"),
                sessionState,
            };
            Assert.AreEqual(JsonSerializer.Serialize(expectedBody), this.GetResponseBody());
        }

        [TestMethod]
        public async Task HandleRequest_Anonymous()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal();
            var ticket = new AuthenticationTicket(claimsPrincipal, null, BffConstant.AuthenticationSchemeCookie);
            var authenticateResult = AuthenticateResult.Success(ticket);
            this.mockAuthenticationService.Setup(m =>
                    m.AuthenticateAsync(this.context, BffConstant.AuthenticationSchemeCookie)
                )
                .ReturnsAsync(authenticateResult);
            this.context.Request.Method = "GET";
            var userInfoHandler = new UserInfoHandler(
                this.mockBffClientResolver.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object
            );
            // Act
            var result = await userInfoHandler.HandleRequest(this.context);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, this.context.Response.StatusCode);
            var expectedBody = new { isAuthenticated = false };
            Assert.AreEqual(JsonSerializer.Serialize(expectedBody), this.GetResponseBody());
        }

        [TestMethod]
        public async Task HandleRequest_DiscoveryUnavailable()
        {
            // Arrange
            var claimsIdentity = new ClaimsIdentity([], BffConstant.AuthenticationSchemeCookie);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var ticket = new AuthenticationTicket(claimsPrincipal, null, BffConstant.AuthenticationSchemeCookie);
            var authenticateResult = AuthenticateResult.Success(ticket);
            this.mockAuthenticationService.Setup(m =>
                    m.AuthenticateAsync(this.context, BffConstant.AuthenticationSchemeCookie)
                )
                .ReturnsAsync(authenticateResult);
            this.context.Request.Method = "GET";
            var userInfoHandler = new UserInfoHandler(
                this.mockBffClientResolver.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object
            );
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.bffClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync((Discovery.Discovery?)null);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await userInfoHandler.HandleRequest(this.context)
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains("Unable to resolve discovery", exception.Message);
        }

        private string GetResponseBody()
        {
            using var reader = new StreamReader(this.context.Response.Body, Encoding.UTF8);
            this.context.Response.Body.Seek(0, SeekOrigin.Begin);
            return reader.ReadToEnd();
        }
    }
}
