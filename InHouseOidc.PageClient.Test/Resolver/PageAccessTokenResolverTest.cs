// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Type;
using InHouseOidc.Discovery;
using InHouseOidc.PageClient;
using InHouseOidc.PageClient.Resolver;
using InHouseOidc.PageClient.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace InHouseOidc.PageClient.Test.Resolver
{
    [TestClass]
    public class PageAccessTokenResolverTest
    {
        private const string OidcProviderAddress = "https://localhost";

        private readonly Mock<IHttpContextAccessor> mockHttpContextAccessor = new(MockBehavior.Strict);
        private readonly Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        private readonly Mock<IAuthenticationService> mockAuthenticationService = new(MockBehavior.Strict);
        private readonly TestLogger<PageAccessTokenResolver> logger = new();
        private readonly DateTimeOffset utcNow = new DateTimeOffset(
            2022,
            4,
            23,
            18,
            22,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();
        private readonly string clientName = "testclient";
        private readonly TokenResponse tokenResponse =
            new()
            {
                AccessToken = "accesstoken",
                ExpiresIn = 600,
                RefreshToken = "refreshtoken",
            };

        private ClientOptions clientOptions = new();
        private Discovery.Discovery? discovery;
        private Mock<IDiscoveryResolver> mockDiscoveryResolver = new(MockBehavior.Strict);
        private Mock<IHttpClientFactory> mockHttpClientFactory = new(MockBehavior.Strict);
        private TestMessageHandler testMessageHandler = new();
        private Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.clientOptions = new();
            this.mockDiscoveryResolver = new(MockBehavior.Strict);
            this.mockHttpClientFactory = new(MockBehavior.Strict);
            this.testMessageHandler = new TestMessageHandler();
            this.mockHttpClientFactory
                .Setup(m => m.CreateClient(this.clientOptions.InternalHttpClientName))
                .Returns(new HttpClient(this.testMessageHandler));
            this.mockUtcNow = new Mock<IUtcNow>(MockBehavior.Strict);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
            this.clientOptions.PageClientOptions = new PageClientOptions
            {
                ClientId = "pageclientid",
                OidcProviderAddress = OidcProviderAddress,
                Scope = "pagescope",
            };
            Assert.IsNotNull(this.clientOptions.PageClientOptions.OidcProviderAddress);
            this.discovery = new Discovery.Discovery(
                null,
                null,
                DateTimeOffset.MaxValue,
                new List<string> { "code" },
                this.clientOptions.PageClientOptions.OidcProviderAddress,
                "/token",
                new List<string> { DiscoveryConstant.ClientSecretPost }
            );
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_RefreshSuccess()
        {
            // Arrange
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            this.mockDiscoveryResolver
                .Setup(
                    m =>
                        m.GetDiscovery(this.clientOptions.DiscoveryOptions, OidcProviderAddress, CancellationToken.None)
                )
                .ReturnsAsync(this.discovery);
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(this.tokenResponse),
                StatusCode = HttpStatusCode.OK,
            };
            this.SetupHttpContext("at", this.utcNow, "rt1", out var resultAuthenticationProperties1);
            // Act 1 (expired)
            var result1 = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert 1
            Assert.AreEqual(this.tokenResponse.AccessToken, result1);
            Assert.AreEqual(
                this.tokenResponse.AccessToken,
                resultAuthenticationProperties1.Items[".Token.access_token"]
            );
            var expiresIn1 = this.utcNow.UtcDateTime.AddSeconds(this.tokenResponse.ExpiresIn ?? 0).ToString("o");
            Assert.AreEqual(expiresIn1, resultAuthenticationProperties1.Items[".Token.expires_at"]);
            Assert.AreEqual(
                this.tokenResponse.RefreshToken,
                resultAuthenticationProperties1.Items[".Token.refresh_token"]
            );
            // Act 2 (missing expiry)
            this.SetupHttpContext("at", null, "rt2", out var resultAuthenticationProperties2);
            var result2 = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert 2
            Assert.AreEqual(this.tokenResponse.AccessToken, result2);
            Assert.AreEqual(
                this.tokenResponse.AccessToken,
                resultAuthenticationProperties2.Items[".Token.access_token"]
            );
            var expiresIn2 = this.utcNow.UtcDateTime.AddSeconds(this.tokenResponse.ExpiresIn ?? 0).ToString("o");
            Assert.AreEqual(expiresIn2, resultAuthenticationProperties2.Items[".Token.expires_at"]);
            Assert.AreEqual(
                this.tokenResponse.RefreshToken,
                resultAuthenticationProperties2.Items[".Token.refresh_token"]
            );
            // Act 3 (missing access token and expiry)
            this.SetupHttpContext(null, null, "rt3", out var resultAuthenticationProperties3);
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert 2
            Assert.AreEqual(this.tokenResponse.AccessToken, result);
            Assert.AreEqual(
                this.tokenResponse.AccessToken,
                resultAuthenticationProperties2.Items[".Token.access_token"]
            );
            var expiresIn3 = this.utcNow.UtcDateTime.AddSeconds(this.tokenResponse.ExpiresIn ?? 0).ToString("o");
            Assert.AreEqual(expiresIn3, resultAuthenticationProperties2.Items[".Token.expires_at"]);
            Assert.AreEqual(
                this.tokenResponse.RefreshToken,
                resultAuthenticationProperties2.Items[".Token.refresh_token"]
            );
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_NotExpired()
        {
            // Arrange
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            var accessToken = "notexpired";
            this.SetupHttpContext(accessToken, this.utcNow.AddHours(1).ToUniversalTime(), "rt", out var _);
            // Act
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.AreEqual(accessToken, result);
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(0, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_NoHttpContext()
        {
            // Arrange
            this.mockHttpContextAccessor.Setup(m => m.HttpContext).Returns((HttpContext?)null);
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            // Act
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(0, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_NotAuthenticated()
        {
            // Arrange
            var authenticateResult = AuthenticateResult.Fail(new Exception());
            var httpContext = new DefaultHttpContext();
            this.mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
            this.mockAuthenticationService
                .Setup(m => m.AuthenticateAsync(httpContext, null))
                .ReturnsAsync(authenticateResult);
            httpContext.RequestServices = this.mockServiceProvider.Object;
            this.mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(this.mockAuthenticationService.Object);
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            // Act
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.logger.AssertLastItemContains(LogLevel.Information, "User not authenticated");
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(0, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_RefreshTokenAbsent()
        {
            // Arrange
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            this.SetupHttpContext("at", new DateTimeOffset(2022, 4, 23, 18, 22, 00, TimeSpan.Zero), null, out var _);
            // Act
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.logger.AssertLastItemContains(LogLevel.Information, "Refresh token not found");
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(0, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_BadClientConfig()
        {
            // Arrange
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            this.SetupHttpContext("at", new DateTimeOffset(2022, 4, 23, 18, 22, 00, TimeSpan.Zero), "rt", out var _);
            this.clientOptions.PageClientOptions = null;
            // Act
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.logger.AssertLastItemContains(LogLevel.Error, "PageClientOptions incorrectly configured");
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(0, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_DiscoveryUnavailable()
        {
            // Arrange
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            this.mockDiscoveryResolver
                .Setup(
                    m =>
                        m.GetDiscovery(this.clientOptions.DiscoveryOptions, OidcProviderAddress, CancellationToken.None)
                )
                .ReturnsAsync((Discovery.Discovery?)null);
            this.SetupHttpContext("at", this.utcNow, "rt1", out var resultAuthenticationProperties1);
            // Act
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(0, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_TokenGetBadRequest()
        {
            // Arrange
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            this.mockDiscoveryResolver
                .Setup(
                    m =>
                        m.GetDiscovery(this.clientOptions.DiscoveryOptions, OidcProviderAddress, CancellationToken.None)
                )
                .ReturnsAsync(this.discovery);
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
            };
            this.SetupHttpContext("at", this.utcNow, "rt1", out var resultAuthenticationProperties1);
            // Act
            var result = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            this.logger.AssertLastItemContains(LogLevel.Error, "response BadRequest");
        }

        [TestMethod]
        public async Task PageAccessTokenResolver_BadTokenResponse()
        {
            // Arrange
            var pageAccessTokenResolver = new PageAccessTokenResolver(
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.mockHttpContextAccessor.Object,
                this.logger,
                this.mockUtcNow.Object
            );
            this.mockDiscoveryResolver
                .Setup(
                    m =>
                        m.GetDiscovery(this.clientOptions.DiscoveryOptions, OidcProviderAddress, CancellationToken.None)
                )
                .ReturnsAsync(this.discovery);
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(null),
                StatusCode = HttpStatusCode.OK,
            };
            this.SetupHttpContext("at", this.utcNow, "rt1", out var resultAuthenticationProperties1);
            // Act 1 (null response)
            var result1 = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result1);
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid token");
            // Act 2 (no access token)
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(new TokenResponse()),
                StatusCode = HttpStatusCode.OK,
            };
            var result2 = await pageAccessTokenResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result2);
            this.mockDiscoveryResolver.VerifyAll();
            Assert.AreEqual(2, this.testMessageHandler.SendCount);
            this.logger.AssertLastItemContains(LogLevel.Error, "Invalid token");
        }

        private void SetupHttpContext(
            string? accessToken,
            DateTimeOffset? expiresAt,
            string? refreshToken,
            out AuthenticationProperties resultProperties
        )
        {
            var httpContext = new DefaultHttpContext();
            var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var properties = new Dictionary<string, string?>
            {
                { ".TokenNames", "access_token;expires_at;refresh_token" },
            };
            if (!string.IsNullOrEmpty(accessToken))
            {
                properties.Add(".Token.access_token", accessToken);
            }
            if (expiresAt.HasValue)
            {
                properties.Add(".Token.expires_at", expiresAt.Value.ToString("o"));
            }
            if (!string.IsNullOrEmpty(refreshToken))
            {
                properties.Add(".Token.refresh_token", refreshToken);
            }
            var authenticationProperties = new AuthenticationProperties(properties);
            var authenticateResult = AuthenticateResult.Success(
                new AuthenticationTicket(
                    claimsPrincipal,
                    authenticationProperties,
                    CookieAuthenticationDefaults.AuthenticationScheme
                )
            );
            this.mockAuthenticationService
                .Setup(m => m.AuthenticateAsync(httpContext, null))
                .ReturnsAsync(authenticateResult);
            AuthenticationProperties localAuthenticateProperties = new();
            this.mockAuthenticationService
                .Setup(
                    m =>
                        m.SignInAsync(
                            httpContext,
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            claimsPrincipal,
                            authenticationProperties
                        )
                )
                .Callback(
                    (
                        HttpContext _,
                        string _,
                        ClaimsPrincipal _,
                        AuthenticationProperties callbackAuthenticationProperties
                    ) =>
                    {
                        foreach (var item in callbackAuthenticationProperties.Items)
                        {
                            localAuthenticateProperties.Items.Add(item);
                        }
                    }
                )
                .Returns(Task.CompletedTask);
            this.mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(this.mockAuthenticationService.Object);
            httpContext.RequestServices = this.mockServiceProvider.Object;
            this.mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(httpContext);
            resultProperties = localAuthenticateProperties;
        }
    }
}
