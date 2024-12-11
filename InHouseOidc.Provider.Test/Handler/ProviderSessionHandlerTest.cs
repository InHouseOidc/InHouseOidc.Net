// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class ProviderSessionHandlerTest
    {
        private readonly Mock<ICodeStore> mockCodeStore = new(MockBehavior.Strict);
        private readonly ProviderOptions providerOptions = new();
        private readonly Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private readonly DateTimeOffset utcNow = new DateTimeOffset(
            2022,
            5,
            12,
            17,
            33,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();
        private readonly Mock<IValidationHandler> mockValidationHandler = new(MockBehavior.Strict);
        private readonly string host = "localhost";
        private readonly string sessionId = "sessionid";
        private readonly string subject = "subject";
        private readonly string urlScheme = "https";
        private string issuer = string.Empty;

        private Mock<IHttpContextAccessor> mockHttpContextAccessor = new(MockBehavior.Strict);
        private DefaultHttpContext context = new();

        [TestInitialize]
        public void Initialize()
        {
            this.context = new();
            this.context.Request.Headers.Host = this.host;
            this.context.Request.Method = "GET";
            this.context.Request.Scheme = this.urlScheme;
            this.mockHttpContextAccessor = new(MockBehavior.Strict);
            this.mockHttpContextAccessor.Setup(m => m.HttpContext).Returns(this.context);
            this.issuer = $"{this.urlScheme}://{this.host}";
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
        }

        [DataTestMethod]
        [DataRow(true, false, true, true)]
        [DataRow(true, true, true, false)]
        [DataRow(false, false, false, false)]
        public async Task GetLogoutRequest(bool returnStoredCode, bool isExpired, bool returnContent, bool expectResult)
        {
            // Arrange
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var logoutCode = "abc123";
            var logoutRequest = returnContent ? new LogoutRequest { Subject = this.subject } : null;
            var storedCode = returnStoredCode
                ? new StoredCode(
                    logoutCode,
                    CodeType.LogoutCode,
                    JsonSerializer.Serialize(logoutRequest),
                    this.issuer,
                    this.subject
                )
                : null;
            if (returnStoredCode && storedCode != null)
            {
                storedCode.Expiry = this.utcNow.AddMinutes(isExpired ? -1 : 1);
                this.mockCodeStore.Setup(m => m.DeleteCode(logoutCode, CodeType.LogoutCode, this.issuer))
                    .Returns(Task.CompletedTask);
            }
            this.mockCodeStore.Setup(m => m.GetCode(logoutCode, CodeType.LogoutCode, this.issuer))
                .ReturnsAsync(storedCode);
            // Act
            var result = await providerSessionHandler.GetLogoutRequest(logoutCode);
            // Assert
            if (expectResult)
            {
                Assert.IsNotNull(result);
                Assert.AreEqual(JsonSerializer.Serialize(logoutRequest), JsonSerializer.Serialize(result));
            }
            else
            {
                Assert.IsNull(result);
            }
        }

        [TestMethod]
        public async Task GetLogoutRequest_Exception()
        {
            // Arrange
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            this.mockHttpContextAccessor.Setup(m => m.HttpContext).Returns((HttpContext?)null);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await providerSessionHandler.GetLogoutRequest("anycode")
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains(exception.Message, "No HttpContext found to logout");
        }

        [DataTestMethod]
        [DataRow("/rubbish", false, false)]
        [DataRow("/rubbish?param=value", false, false)]
        [DataRow("/connect/authorize?param=value", false, false)]
        [DataRow("/connect/authorize?param=value", true, true)]
        public async Task IsValidReturnUrl(string returnUrl, bool isValidReturnUrl, bool expectedResult)
        {
            // Arrange
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            this.mockValidationHandler.Setup(m =>
                    m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>())
                )
                .ReturnsAsync(
                    (
                        (AuthorizationRequest?)null,
                        isValidReturnUrl ? null : new RedirectError(RedirectErrorType.ServerError, "error")
                    )
                );
            // Act
            var result = await providerSessionHandler.IsValidReturnUrl(returnUrl);
            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task IsValidReturnUrl_AuthorizationCodeNotEnabled()
        {
            // Arrange
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await providerSessionHandler.IsValidReturnUrl("/rubbish?param=value")
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains("AuthorizationCode flow not enabled", exception.Message);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Login(bool checkSessionEndpointEnabled)
        {
            // Arrange
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var mockAuthenticationService = new Mock<IAuthenticationService>(MockBehavior.Strict);
            mockAuthenticationService
                .Setup(m =>
                    m.SignInAsync(
                        this.context,
                        ProviderConstant.AuthenticationSchemeCookie,
                        It.IsAny<ClaimsPrincipal>(),
                        It.IsAny<AuthenticationProperties>()
                    )
                )
                .Returns(Task.CompletedTask);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthenticationService.Object);
            this.context.RequestServices = mockServiceProvider.Object;
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var claims = new List<Claim> { new(JsonWebTokenClaim.Subject, this.subject) };
            this.providerOptions.CheckSessionEndpointEnabled = checkSessionEndpointEnabled;
            // Act
            var result = await providerSessionHandler.Login(this.context, claims, TimeSpan.FromMinutes(60));
            // Assert
            Assert.IsNotNull(result);
            var expectedClaimTypes = new List<string>
            {
                JsonWebTokenClaim.Subject,
                JsonWebTokenClaim.AuthenticationTime,
                JsonWebTokenClaim.IdentityProvider,
                JsonWebTokenClaim.SessionId,
            };
            CollectionAssert.AreEqual(expectedClaimTypes, result.Claims.Select(c => c.Type).ToList());
            if (checkSessionEndpointEnabled)
            {
                var cookie = this.context.Response.Headers.SetCookie;
                Assert.IsNotNull(cookie);
            }
            mockAuthenticationService.VerifyAll();
        }

        [TestMethod]
        public async Task Login_AuthorizationCodeNotEnabled()
        {
            // Arrange
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var claims = new List<Claim>();
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await providerSessionHandler.Login(this.context, claims, TimeSpan.FromMinutes(60))
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains("AuthorizationCode flow not enabled", exception.Message);
        }

        [DataTestMethod]
        [DataRow(true, true, true, true, true)]
        [DataRow(true, true, true, true, false)]
        [DataRow(false, true, true, false, false)]
        public async Task Logout(
            bool passLogoutRequest,
            bool enableCheckSessionEndpoint,
            bool passLogoutCode,
            bool setPLRU,
            bool setState
        )
        {
            // Arrange
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var mockAuthenticationService = new Mock<IAuthenticationService>(MockBehavior.Strict);
            var authenticationPropertiesCaptured = (AuthenticationProperties?)null;
            mockAuthenticationService
                .Setup(m =>
                    m.SignOutAsync(
                        this.context,
                        ProviderConstant.AuthenticationSchemeCookie,
                        It.IsAny<AuthenticationProperties>()
                    )
                )
                .Callback(
                    (HttpContext context, string? scheme, AuthenticationProperties? properties) =>
                        authenticationPropertiesCaptured = properties
                )
                .Returns(Task.CompletedTask);
            if (enableCheckSessionEndpoint)
            {
                this.providerOptions.CheckSessionEndpointEnabled = true;
                this.context.Response.Cookies.Append(this.providerOptions.CheckSessionCookieName, this.sessionId);
            }
            var logoutCode = (string?)null;
            if (passLogoutCode)
            {
                logoutCode = "logmeout";
                this.mockCodeStore.Setup(m => m.DeleteCode(logoutCode, CodeType.LogoutCode, this.issuer))
                    .Returns(Task.CompletedTask);
            }
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthenticationService.Object);
            this.context.RequestServices = mockServiceProvider.Object;
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var plru = "https://ui.app/logoutcallback";
            var logoutRequest = passLogoutRequest
                ? new LogoutRequest
                {
                    PostLogoutRedirectUri = setPLRU ? plru : null,
                    State = setState ? "statevalue" : null,
                }
                : null;
            // Act
            await providerSessionHandler.Logout(this.context, logoutCode, logoutRequest);
            // Assert
            mockAuthenticationService.VerifyAll();
            if (enableCheckSessionEndpoint)
            {
                var cookie = this.context.Response.Headers.SetCookie.Single();
                Assert.IsNotNull(cookie);
                StringAssert.Contains(cookie, this.providerOptions.CheckSessionCookieName);
                StringAssert.Contains(cookie, "expires=");
            }
            if (passLogoutCode)
            {
                this.mockCodeStore.VerifyAll();
            }
            if (setPLRU)
            {
                Assert.IsNotNull(authenticationPropertiesCaptured);
                StringAssert.StartsWith(authenticationPropertiesCaptured.RedirectUri, plru);
            }
            if (setState)
            {
                Assert.IsNotNull(authenticationPropertiesCaptured);
                StringAssert.EndsWith(authenticationPropertiesCaptured.RedirectUri, "?state=statevalue");
            }
        }

        [TestMethod]
        public async Task Logout_AuthorizationCodeNotEnabled()
        {
            // Arrange
            var providerSessionHandler = new ProviderSessionHandler(
                this.mockCodeStore.Object,
                this.mockHttpContextAccessor.Object,
                this.providerOptions,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var claims = new List<Claim>();
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await providerSessionHandler.Logout(this.context, null, null)
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains("AuthorizationCode flow not enabled", exception.Message);
        }
    }
}
