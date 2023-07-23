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
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class AuthorizationHandlerTest
    {
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
        private readonly string clientId = "client";
        private readonly string host = "localhost";
        private readonly string redirectUri = "https://client.app/auth/callback";
        private readonly string scheme = "scheme";
        private readonly string scope = "scope";
        private readonly string sessionId = "sessionid";
        private readonly string state = "statevalue";
        private readonly string subject = "subject";
        private readonly string urlScheme = "https";

        private Mock<ICodeStore> mockCodeStore = new(MockBehavior.Strict);
        private Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private ProviderOptions providerOptions = new();

        [TestInitialize]
        public void Initialise()
        {
            this.mockCodeStore = new Mock<ICodeStore>(MockBehavior.Strict);
            this.mockUtcNow = new Mock<IUtcNow>(MockBehavior.Strict);
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
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            )
            {
                State = this.state,
            };
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            var storedCodeCaptured = (StoredCode?)null;
            this.mockCodeStore
                .Setup(m => m.SaveCode(It.IsAny<StoredCode>()))
                .Callback((StoredCode storedCodePassed) => storedCodeCaptured = storedCodePassed)
                .Returns(Task.CompletedTask);
            // Act
            var result = await authorizationHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(302, context.Response.StatusCode);
            var location = context.Response.Headers.Location.First();
            var redirectUri = new Uri(location);
            Assert.AreEqual(this.urlScheme, redirectUri.Scheme);
            StringAssert.StartsWith(redirectUri.OriginalString, this.redirectUri);
            var queryParams = HttpUtility.ParseQueryString(redirectUri.Query);
            CollectionAssert.AreEquivalent(new List<string> { "code", "scope", "state" }, queryParams.Keys);
            Assert.AreEqual(queryParams["scope"], this.scope);
            Assert.AreEqual(queryParams["state"], this.state);
            Assert.IsTrue((queryParams["code"]?.Length ?? 0) > 80);
            Assert.IsNotNull(storedCodeCaptured);
            Assert.AreEqual(queryParams["code"], storedCodeCaptured.Code);
            Assert.AreEqual(CodeType.AuthorizationCode, storedCodeCaptured.CodeType);
            Assert.AreEqual(this.utcNow.AddMinutes(5), storedCodeCaptured.Expiry);
            Assert.AreEqual($"https://{this.host}", storedCodeCaptured.Issuer);
            Assert.AreEqual(this.subject, storedCodeCaptured.Subject);
            Assert.AreEqual(
                JsonSerializer.Serialize(authorizationRequest, JsonHelper.JsonSerializerOptions),
                storedCodeCaptured.Content
            );
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
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await authorizationHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("HttpMethod not supported: {method}", exception.LogMessage);
        }

        [DataTestMethod]
        [DataRow("GET")]
        [DataRow("POST")]
        public async Task HandleRequest_NoParameters(string method)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = method;
            context.Request.Scheme = this.urlScheme;
            if (context.Request.Method == "POST")
            {
                context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            }
            var serviceProvider = new TestServiceCollection().BuildServiceProvider();
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await authorizationHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Unable to resolve authorization request parameters", exception.LogMessage);
        }

        [DataTestMethod]
        [DataRow(false, true, true)]
        [DataRow(false, true, false)]
        [DataRow(false, false, false)]
        public async Task HandleRequest_ValidationFailure(bool returnResults, bool returnError, bool errorRedirect)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}");
            var serviceProvider = new TestServiceCollection().BuildServiceProvider();
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var errorLogged = (string?)null;
            if (returnResults)
            {
                var authorizationRequest = new AuthorizationRequest(
                    this.clientId,
                    this.redirectUri,
                    ResponseType.Code,
                    this.scope
                )
                {
                    State = this.state,
                };
                this.mockValidationHandler
                    .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync((authorizationRequest, null));
            }
            else if (returnError)
            {
                errorLogged = "A message {parameter}";
                var authorizationError = new RedirectError(RedirectErrorType.ServerError, errorLogged, "value");
                if (errorRedirect)
                {
                    authorizationError.RedirectUri = this.redirectUri;
                }
                this.mockValidationHandler
                    .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync((null, authorizationError));
            }
            else
            {
                errorLogged = "Unable to parse and validate authorization request";
                this.mockValidationHandler
                    .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                    .ReturnsAsync((null, null));
            }
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await authorizationHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(errorLogged, exception.LogMessage);
            if (errorRedirect)
            {
                Assert.AreEqual(this.redirectUri, exception.Uri);
            }
        }

        [TestMethod]
        public async Task HandleRequest_PromptLogin()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}");
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
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            )
            {
                Prompt = Prompt.Login,
            };
            authorizationRequest.State = this.state;
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            // Act
            var result = await authorizationHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(302, context.Response.StatusCode);
            var location = context.Response.Headers.Location.First();
            var redirectUri = new Uri(location, UriKind.Relative);
            Assert.IsNotNull(redirectUri);
            var pathQuery = location.Split('?');
            Assert.AreEqual(2, pathQuery.Length);
            Assert.AreEqual("/login", pathQuery[0]);
            var queryParams = HttpUtility.ParseQueryString(pathQuery[1]);
            CollectionAssert.AreEquivalent(new List<string> { "ReturnUrl" }, queryParams.Keys);
            var returnUrl = HttpUtility.UrlDecode(queryParams["ReturnUrl"]);
            Assert.IsNotNull(returnUrl);
            var returnUrlParts = returnUrl.Split('?');
            Assert.AreEqual("/connect/authorize", returnUrlParts[0]);
            var returnUrlQuery = HttpUtility.ParseQueryString(returnUrlParts[1]);
            CollectionAssert.AreEquivalent(new List<string> { "state" }, returnUrlQuery.Keys);
        }

        [TestMethod]
        public async Task HandleRequest_PromptNone()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}");
            var serviceCollection = new TestServiceCollection();
            TestHelper.SetupContextClaimsPrincipal(
                context,
                serviceCollection,
                false,
                TimeSpan.Zero,
                this.scheme,
                this.subject,
                this.sessionId,
                this.utcNow
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            )
            {
                Prompt = Prompt.None,
            };
            authorizationRequest.State = this.state;
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await authorizationHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Login required with prompt=none", exception.LogMessage);
            Assert.AreEqual(this.redirectUri, exception.Uri);
        }

        [TestMethod]
        public async Task HandleRequest_PromptNotSpecified()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}");
            var serviceCollection = new TestServiceCollection();
            TestHelper.SetupContextClaimsPrincipal(
                context,
                serviceCollection,
                false,
                TimeSpan.Zero,
                this.scheme,
                this.subject,
                this.sessionId,
                this.utcNow
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            )
            {
                Prompt = Prompt.NotSpecified,
            };
            authorizationRequest.State = this.state;
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await authorizationHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Unknown Prompt: {prompt}", exception.LogMessage);
            Assert.AreEqual(this.redirectUri, exception.Uri);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task HandleRequest_MaxAge(bool isAgePassed)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}&prompt=none");
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
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            )
            {
                MaxAge = isAgePassed ? 60 : 120,
            };
            authorizationRequest.State = this.state;
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow.AddSeconds(90));
            // Act
            var result = await authorizationHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(302, context.Response.StatusCode);
            var location = context.Response.Headers.Location.First();
            if (isAgePassed)
            {
                var redirectUri = new Uri(location, UriKind.Relative);
                Assert.IsNotNull(redirectUri);
                var pathQuery = location.Split('?');
                Assert.AreEqual(2, pathQuery.Length);
                Assert.AreEqual("/login", pathQuery[0]);
                var queryParams = HttpUtility.ParseQueryString(pathQuery[1]);
                CollectionAssert.AreEquivalent(new List<string> { "ReturnUrl" }, queryParams.Keys);
                var returnUrl = HttpUtility.UrlDecode(queryParams["ReturnUrl"]);
                Assert.IsNotNull(returnUrl);
                var returnUrlParts = returnUrl.Split('?');
                Assert.AreEqual("/connect/authorize", returnUrlParts[0]);
                var returnUrlQuery = HttpUtility.ParseQueryString(returnUrlParts[1]);
                CollectionAssert.AreEquivalent(new List<string> { "state" }, returnUrlQuery.Keys);
            }
            else
            {
                var redirectUri = new Uri(location, UriKind.Absolute);
                Assert.IsNotNull(redirectUri);
                StringAssert.StartsWith(redirectUri.OriginalString, this.redirectUri);
                var queryParams = HttpUtility.ParseQueryString(redirectUri.Query);
                CollectionAssert.AreEquivalent(new List<string> { "code", "scope", "state" }, queryParams.Keys);
            }
        }

        [DataTestMethod]
        [DataRow(false, "", "Invalid id token hint")]
        [DataRow(true, "other", "Id token hint subject does not match authenticated subject")]
        [DataRow(true, "subject", null)]
        public async Task IdTokenHint(bool issueValidJwt, string jwtSubject, string expectedExceptionMessage)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}");
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
            var (tokenClaimsPrincipal, _) = TestHelper.SetupClaimsPrincipal(
                TimeSpan.Zero,
                this.scheme,
                jwtSubject,
                this.sessionId,
                this.utcNow
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var jwt = "abc";
            var issuer = $"https://{this.host}";
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            )
            {
                IdTokenHint = jwt,
            };
            this.mockValidationHandler
                .Setup(m => m.ValidateJsonWebToken(null, issuer, jwt, true))
                .Returns(issueValidJwt ? tokenClaimsPrincipal : null);
            authorizationRequest.State = this.state;
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            // Act/Assert
            if (string.IsNullOrEmpty(expectedExceptionMessage))
            {
                var result = await authorizationHandler.HandleRequest(context.Request);
                Assert.IsTrue(result);
                Assert.AreEqual(302, context.Response.StatusCode);
            }
            else
            {
                var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                    async () => await authorizationHandler.HandleRequest(context.Request)
                );
                Assert.IsNotNull(exception);
                Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
                Assert.AreEqual(this.redirectUri, exception.Uri);
            }
        }

        [DataTestMethod]
        [DataRow("sessionid", false)]
        [DataRow("newsessionid", true)]
        public async Task CheckSessionEndpointEnabled(string cookieSessionId, bool expectNewResponseCookie)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}");
            context.Request.Headers["Cookie"] = new[]
            {
                $"{this.providerOptions.CheckSessionCookieName}={cookieSessionId}",
            };
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
            this.providerOptions.CheckSessionEndpointEnabled = true;
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            );
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            // Act
            var result = await authorizationHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(302, context.Response.StatusCode);
            Assert.IsNotNull(authorizationRequest.SessionState);
            var sessionStateParts = authorizationRequest.SessionState.Split(".");
            Assert.AreEqual(2, sessionStateParts.Length);
            Assert.AreEqual(
                HashHelper.GenerateSessionState(sessionStateParts[1], this.clientId, this.redirectUri, this.sessionId),
                authorizationRequest.SessionState
            );
            if (expectNewResponseCookie)
            {
                var responseCookie = context.Response.Headers.First();
                Assert.IsNotNull(responseCookie);
                Assert.AreEqual("Set-Cookie", responseCookie.Key);
                Assert.AreEqual(
                    "InHouseOidc.CheckSession=sessionid; path=/; secure; samesite=none",
                    responseCookie.Value.ToString()
                );
            }
        }

        [TestMethod]
        public async Task HandleRequest_AuthorizationMinimumTokenExpiry()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Request.QueryString = new QueryString($"?state={this.state}");
            var serviceCollection = new TestServiceCollection();
            TestHelper.SetupContextClaimsPrincipal(
                context,
                serviceCollection,
                true,
                TimeSpan.Zero,
                this.scheme,
                this.subject,
                this.sessionId,
                this.utcNow,
                TimeSpan.FromSeconds(30)
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var authorizationHandler = new AuthorizationHandler(
                this.mockCodeStore.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object,
                this.mockValidationHandler.Object
            );
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope
            )
            {
                Prompt = Prompt.None,
            };
            authorizationRequest.State = this.state;
            this.mockValidationHandler
                .Setup(m => m.ParseValidateAuthorizationRequest(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync((authorizationRequest, null));
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>())).Returns(Task.CompletedTask);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<RedirectErrorException>(
                async () => await authorizationHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Login required as session is near expiry", exception.LogMessage);
            Assert.AreEqual(this.redirectUri, exception.Uri);
        }
    }
}
