// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.PageClient.Test
{
    [TestClass]
    public class HttpContextExtensionTest
    {
        private readonly string redirectUri = "https://client.app";

        [TestMethod]
        public async Task PageClientLogout()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var mockAuthenticationService = new Mock<IAuthenticationService>(MockBehavior.Strict);
            var authenticationPropertiesCaptured = (AuthenticationProperties?)null;
            mockAuthenticationService
                .Setup(m =>
                    m.SignOutAsync(
                        context,
                        PageConstant.AuthenticationSchemeCookie,
                        It.IsAny<AuthenticationProperties>()
                    )
                )
                .Returns(Task.CompletedTask);
            mockAuthenticationService
                .Setup(m =>
                    m.SignOutAsync(
                        context,
                        OpenIdConnectDefaults.AuthenticationScheme,
                        It.IsAny<AuthenticationProperties>()
                    )
                )
                .Callback(
                    (HttpContext context, string? scheme, AuthenticationProperties? properties) =>
                        authenticationPropertiesCaptured = properties
                )
                .Returns(Task.CompletedTask);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthenticationService.Object);
            context.RequestServices = mockServiceProvider.Object;
            // Act
            await context.PageClientLogout(this.redirectUri);
            // Assert
            mockAuthenticationService.VerifyAll();
            Assert.IsNotNull(authenticationPropertiesCaptured);
            StringAssert.StartsWith(authenticationPropertiesCaptured.RedirectUri, this.redirectUri);
        }

        [TestMethod]
        public async Task ProviderPageClientLogout()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var mockServiceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            var mockAuthenticationService = new Mock<IAuthenticationService>(MockBehavior.Strict);
            var authenticationPropertiesCaptured = (AuthenticationProperties?)null;
            mockAuthenticationService
                .Setup(m =>
                    m.SignOutAsync(
                        context,
                        OpenIdConnectDefaults.AuthenticationScheme,
                        It.IsAny<AuthenticationProperties>()
                    )
                )
                .Callback(
                    (HttpContext context, string? scheme, AuthenticationProperties? properties) =>
                        authenticationPropertiesCaptured = properties
                )
                .Returns(Task.CompletedTask);
            mockServiceProvider
                .Setup(p => p.GetService(typeof(IAuthenticationService)))
                .Returns(mockAuthenticationService.Object);
            context.RequestServices = mockServiceProvider.Object;
            // Act
            await context.ProviderPageClientLogout(this.redirectUri);
            // Assert
            mockAuthenticationService.VerifyAll();
            Assert.IsNotNull(authenticationPropertiesCaptured);
            StringAssert.StartsWith(authenticationPropertiesCaptured.RedirectUri, this.redirectUri);
        }
    }
}
