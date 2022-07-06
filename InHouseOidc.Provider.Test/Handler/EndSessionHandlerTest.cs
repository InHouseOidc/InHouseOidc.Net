// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class EndSessionHandlerTest
    {
        private readonly Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private readonly Mock<IValidationHandler> mockValidationHandler = new(MockBehavior.Strict);
        private readonly string host = "localhost";
        private readonly string postLogoutRedirectUri = "/logout/callback";
        private readonly string scheme = "scheme";
        private readonly string sessionId = "sessionid";
        private readonly string state = "statevalue";
        private readonly string subject = "subject";
        private readonly string urlScheme = "https";
        private readonly DateTimeOffset utcNow = new DateTimeOffset(
            2022,
            5,
            16,
            17,
            00,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();

        private Mock<IClientStore> mockClientStore = new(MockBehavior.Strict);
        private Mock<ICodeStore> mockCodeStore = new(MockBehavior.Strict);
        private ProviderOptions providerOptions = new();

        [TestInitialize]
        public void Initialise()
        {
            this.mockClientStore = new Mock<IClientStore>(MockBehavior.Strict);
            this.mockCodeStore = new Mock<ICodeStore>(MockBehavior.Strict);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
            this.providerOptions = new();
        }

        [DataTestMethod]
        [DataRow("GET")]
        [DataRow("POST")]
        public async Task HandleRequest_Success(string method)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = method;
            context.Request.Scheme = this.urlScheme;
            if (context.Request.Method == "GET")
            {
                context.Request.QueryString = new QueryString($"?state={this.state}");
            }
            else
            {
                context.Request.ContentType = ContentTypeConstant.ApplicationForm;
                context.Request.Body = new FormUrlEncodedContent(
                    new Dictionary<string, string> { { "state", this.state } }
                ).ReadAsStream();
            }
            var serviceCollection = new TestServiceCollection();
            TestHelper.SetupContextClaimsPrincipal(
                context,
                serviceCollection,
                true,
                TimeSpan.Zero,
                this.scheme,
                this.subject,
                this.sessionId,
                this.utcNow
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            var endSessionHandler = new EndSessionHandler(
                this.mockClientStore.Object,
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            // Act
            var result = await endSessionHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(302, context.Response.StatusCode);
            var location = context.Response.Headers.Location.First();
            StringAssert.StartsWith(location, "/logout?");
        }

        [TestMethod]
        public async Task HandleRequest_InvalidMethod()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "DELETE";
            context.Request.Scheme = this.urlScheme;
            var serviceProvider = new TestServiceCollection().BuildServiceProvider();
            var endSessionHandler = new EndSessionHandler(
                this.mockClientStore.Object,
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await endSessionHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("HttpMethod not supported: {method}", exception.LogMessage);
        }

        [TestMethod]
        public async Task HandleRequest_NoParameters()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            var serviceProvider = new TestServiceCollection().BuildServiceProvider();
            var endSessionHandler = new EndSessionHandler(
                this.mockClientStore.Object,
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await endSessionHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Unable to resolve end session parameters", exception.LogMessage);
        }

        [DataTestMethod]
        [DataRow(true, false, "", null, 16, "Invalid id token hint")]
        [DataRow(true, true, "other", null, 16, "Id token hint subject does not match authenticated subject")]
        [DataRow(true, true, "subject", false, 16, "Invalid post_logout_redirect_uri")]
        [DataRow(true, null, "subject", true, 16, "Invalid post_logout_redirect_uri without id_token_hint")]
        [DataRow(true, true, "subject", true, 513, "State exceeds maximum length of 512 characters")]
        [DataRow(false, null, "subject", null, 16, "End session request for unauthenticated user")]
        [DataRow(true, true, "subject", true, null, null)]
        [DataRow(true, true, "subject", true, 0, null)]
        [DataRow(true, true, "subject", true, 16, null)]
        [DataRow(false, true, "subject", true, 16, null)]
        public async Task IdTokenHint(
            bool authenticateSuccess,
            bool? issueValidJwt,
            string jwtSubject,
            bool? isValidPostLogoutRedirectUri,
            int? stateLength,
            string expectedExceptionMessage
        )
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            var jwt = "abc";
            context.Request.QueryString = new();
            if (stateLength.HasValue)
            {
                context.Request.QueryString = context.Request.QueryString.Add(
                    EndSessionEndpointConstant.State,
                    new string('a', stateLength.Value)
                );
            }
            var issuer = $"https://{this.host}";
            if (isValidPostLogoutRedirectUri.HasValue)
            {
                context.Request.QueryString = context.Request.QueryString.Add(
                    EndSessionEndpointConstant.PostLogoutRedirectUri,
                    this.postLogoutRedirectUri
                );
                this.mockClientStore
                    .Setup(m => m.IsKnownPostLogoutRedirectUri(this.postLogoutRedirectUri))
                    .ReturnsAsync(isValidPostLogoutRedirectUri.Value);
            }
            var serviceCollection = new TestServiceCollection();
            TestHelper.SetupContextClaimsPrincipal(
                context,
                serviceCollection,
                authenticateSuccess,
                TimeSpan.Zero,
                this.scheme,
                this.subject,
                this.sessionId,
                this.utcNow
            );
            var (tokenClaimsPrincipal, _) = TestHelper.SetupClaimsPrincipal(
                TimeSpan.Zero,
                this.scheme,
                jwtSubject,
                this.sessionId,
                this.utcNow
            );
            if (issueValidJwt.HasValue)
            {
                context.Request.QueryString = context.Request.QueryString.Add(
                    EndSessionEndpointConstant.IdTokenHint,
                    jwt
                );
                this.mockValidationHandler
                    .Setup(m => m.ValidateJsonWebToken(null, issuer, jwt, false))
                    .Returns(issueValidJwt.Value ? tokenClaimsPrincipal : null);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var storedCodeCaptured = (StoredCode?)null;
            this.mockCodeStore
                .Setup(m => m.SaveCode(It.IsAny<StoredCode>()))
                .Callback((StoredCode storedCodePassed) => storedCodeCaptured = storedCodePassed)
                .Returns(Task.CompletedTask);
            var endSessionHandler = new EndSessionHandler(
                this.mockClientStore.Object,
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            // Act/Assert
            if (string.IsNullOrEmpty(expectedExceptionMessage))
            {
                var result = await endSessionHandler.HandleRequest(context.Request);
                Assert.IsTrue(result);
                Assert.AreEqual(302, context.Response.StatusCode);
                if (authenticateSuccess && (isValidPostLogoutRedirectUri ?? false) && (issueValidJwt ?? false))
                {
                    var location = context.Response.Headers.Location.First();
                    var redirectUri = new Uri(location, UriKind.Relative);
                    Assert.IsNotNull(redirectUri);
                    var pathQuery = location.Split('?');
                    Assert.AreEqual(2, pathQuery.Length);
                    Assert.AreEqual("/logout", pathQuery[0]);
                    var queryParams = HttpUtility.ParseQueryString(pathQuery[1]);
                    Assert.IsNotNull(storedCodeCaptured);
                    Assert.AreEqual(queryParams["logout_code"], storedCodeCaptured.Code);
                    Assert.AreEqual(CodeType.LogoutCode, storedCodeCaptured.CodeType);
                    Assert.AreEqual(this.utcNow.AddMinutes(5), storedCodeCaptured.Expiry);
                    Assert.AreEqual($"https://{this.host}", storedCodeCaptured.Issuer);
                    Assert.AreEqual(this.subject, storedCodeCaptured.Subject);
                }
            }
            else
            {
                var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                    async () => await endSessionHandler.HandleRequest(context.Request)
                );
                Assert.IsNotNull(exception);
                Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
            }
        }
    }
}
