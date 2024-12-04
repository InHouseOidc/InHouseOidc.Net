// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Provider.Test.Extension
{
    [TestClass]
    public class HttpRequestExtensionTest
    {
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task GetClaimsPrincipal_Authenticated(bool isAuthenticated)
        {
            // Arrange
            var context = new DefaultHttpContext();
            var serviceCollection = new TestServiceCollection();
            var mockAuthenticationSchemeProvider = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict);
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationSchemeCookie,
                null,
                typeof(TestHandler)
            );
            mockAuthenticationSchemeProvider
                .Setup(m => m.GetDefaultAuthenticateSchemeAsync())
                .ReturnsAsync(authenticationScheme);
            serviceCollection.AddSingleton(mockAuthenticationSchemeProvider.Object);
            var mockAuthenticationHandler = new Mock<IAuthenticationHandler>(MockBehavior.Strict);
            var claimsIdentity = new ClaimsIdentity(ProviderConstant.AuthenticationSchemeCookie);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string?>());
            var authenticateResult = isAuthenticated
                ? AuthenticateResult.Success(
                    new AuthenticationTicket(
                        claimsPrincipal,
                        authenticationProperties,
                        ProviderConstant.AuthenticationSchemeCookie
                    )
                )
                : AuthenticateResult.NoResult();
            mockAuthenticationHandler.Setup(m => m.AuthenticateAsync()).ReturnsAsync(authenticateResult);
            var mockAuthenticationHandlerProvider = new Mock<IAuthenticationHandlerProvider>(MockBehavior.Strict);
            mockAuthenticationHandlerProvider
                .Setup(m => m.GetHandlerAsync(context, ProviderConstant.AuthenticationSchemeCookie))
                .ReturnsAsync(mockAuthenticationHandler.Object);
            serviceCollection.AddSingleton(mockAuthenticationHandlerProvider.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Act
            var (resultClaimsPrincipal, resultAuthenticationProperties) = await context.Request.GetClaimsPrincipal(
                serviceProvider
            );
            // Assert
            if (isAuthenticated)
            {
                Assert.IsNotNull(resultClaimsPrincipal);
                Assert.IsNotNull(resultAuthenticationProperties);
                Assert.AreEqual(claimsPrincipal, resultClaimsPrincipal);
                Assert.AreEqual(authenticationProperties, resultAuthenticationProperties);
            }
            else
            {
                Assert.IsNull(resultClaimsPrincipal);
                Assert.IsNull(resultAuthenticationProperties);
            }
        }

        [TestMethod]
        public async Task GetClaimsPrincipal_NoDefaultScheme()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var serviceCollection = new TestServiceCollection();
            var mockAuthenticationSchemeProvider = new Mock<IAuthenticationSchemeProvider>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockAuthenticationSchemeProvider.Object);
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationSchemeCookie,
                null,
                typeof(TestHandler)
            );
            mockAuthenticationSchemeProvider
                .Setup(m => m.GetDefaultAuthenticateSchemeAsync())
                .ReturnsAsync(authenticationScheme);
            var mockAuthenticationHandlerProvider = new Mock<IAuthenticationHandlerProvider>(MockBehavior.Strict);
            mockAuthenticationHandlerProvider
                .Setup(m => m.GetHandlerAsync(context, ProviderConstant.AuthenticationSchemeCookie))
                .ReturnsAsync((IAuthenticationHandler?)null);
            serviceCollection.AddSingleton(mockAuthenticationHandlerProvider.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await context.Request.GetClaimsPrincipal(serviceProvider)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Unable to resolve authentication handler for configured scheme", exception.Message);
        }

        [TestMethod]
        public async Task GetFormDictionary_Empty()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            context.Request.Form = new FormCollection([]);
            // Act
            var result = await context.Request.GetFormDictionary();
            // Assert
            Assert.IsNull(result);
        }

        [DataTestMethod]
        [DataRow(null, false)]
        [DataRow("", false)]
        [DataRow("rubbish", false)]
        [DataRow(ContentTypeConstant.ApplicationForm, true)]
        [DataRow($"{ContentTypeConstant.ApplicationForm}; charset=UTF-8", true)]
        public async Task GetFormDictionary(string? contentType, bool expectResult)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.ContentType = contentType;
            context.Request.Form = new FormCollection(
                new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { { "abc", "123" } }
            );
            // Act
            var result = await context.Request.GetFormDictionary();
            // Assert
            if (expectResult)
            {
                Assert.IsNotNull(result);
                Assert.AreEqual("123", result["abc"]);
            }
            else
            {
                Assert.IsNull(result);
            }
        }

        [TestMethod]
        public void GetQueryDictionary()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Query = new QueryCollection(
                new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { { "abc", "123" } }
            );
            // Act
            var result = context.Request.GetQueryDictionary();
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("123", result["abc"]);
        }

        [TestMethod]
        public void GetQueryDictionary_Empty()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Query = new QueryCollection(
                new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()
            );
            // Act
            var result = context.Request.GetQueryDictionary();
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetBaseUri_Good()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Host = new HostString("localhost");
            context.Request.Scheme = "https";
            context.Request.Path = new PathString("/test");
            // Act
            var result = context.Request.GetBaseUriString();
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("https://localhost", result);
        }

        [TestMethod]
        public void GetBaseUri_Bad()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Scheme = "https";
            context.Request.Path = new PathString("/test");
            // Act
            var exception = Assert.ThrowsException<InvalidOperationException>(() => context.Request.GetBaseUriString());
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Unable to resolve from host header", exception.Message);
        }

        private class TestHandler : IAuthenticationHandler
        {
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties? properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties? properties)
            {
                throw new NotImplementedException();
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
