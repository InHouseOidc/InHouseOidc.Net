// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Type;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class TokenHandlerTest
    {
        private readonly Mock<IJsonWebTokenHandler> mockJsonWebTokenHandler = new(MockBehavior.Strict);
        private readonly Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private readonly string clientId = "client";
        private readonly string clientSecret = "topsecret";
        private readonly string codeChallenge = "wv95kX5SYAG4L3CRoJuiSfSYfcf1jM6dUwjn57Ily4I";
        private readonly string codeVerifier = "verifiedby";
        private readonly string host = "localhost";
        private readonly string redirectUri = "https://client.app/auth/callback";
        private readonly string scope1 = "scope1";
        private readonly string scope2 = "scope2";
        private readonly string scopeOfflineAccess = "scope offline_access";
        private readonly string sessionState = "sessionstate";
        private readonly string subject = "sid";
        private readonly string urlScheme = "https";
        private readonly DateTimeOffset utcNow = new DateTimeOffset(
            2022,
            5,
            26,
            7,
            59,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();

        private Mock<IClientStore> mockClientStore = new(MockBehavior.Strict);
        private Mock<ICodeStore> mockCodeStore = new(MockBehavior.Strict);
        private ProviderOptions providerOptions = new();
        private Mock<IUserStore> mockUserStore = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.mockClientStore = new Mock<IClientStore>(MockBehavior.Strict);
            this.mockCodeStore = new Mock<ICodeStore>(MockBehavior.Strict);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
            this.providerOptions = new();
            this.mockUserStore = new Mock<IUserStore>(MockBehavior.Strict);
        }

        [DataTestMethod]
        [DataRow("GET", false, null, "Token request used invalid method: {method}")]
        [DataRow("POST", false, null, "Token request used invalid content type")]
        [DataRow("POST", true, null, "Token request missing grant_type")]
        [DataRow("POST", true, "rubbish", "Unsupported grant_type requested")]
        [DataRow(
            "POST",
            true,
            TokenEndpointConstant.AuthorizationCode,
            "Token request used unsupported grant type: {grantType}"
        )]
        [DataRow(
            "POST",
            true,
            TokenEndpointConstant.ClientCredentials,
            "Token request used unsupported grant type: {grantType}"
        )]
        [DataRow(
            "POST",
            true,
            TokenEndpointConstant.RefreshToken,
            "Token request used unsupported grant type: {grantType}"
        )]
        public async Task HandleRequest_Exceptions(
            string method,
            bool passFormContent,
            string? grantTypeQueryParam,
            string expectedExceptionMessage
        )
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = method;
            context.Request.Scheme = this.urlScheme;
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var tokenHandler = new TokenHandler(
                this.mockClientStore.Object,
                this.mockJsonWebTokenHandler.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            if (passFormContent)
            {
                context.Request.ContentType = ContentTypeConstant.ApplicationForm;
                var formParams = new Dictionary<string, string>()
                {
                    { TokenEndpointConstant.ClientId, this.clientId },
                };
                if (!string.IsNullOrEmpty(grantTypeQueryParam))
                {
                    formParams.Add(TokenEndpointConstant.GrantType, grantTypeQueryParam);
                }
                context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            }
            // Act
            var exception = await Assert.ThrowsExceptionAsync<BadRequestException>(
                async () => await tokenHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task HandleRequest_AuthorizationCode(bool issueRefreshToken)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "POST";
            context.Request.Scheme = this.urlScheme;
            context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            context.Response.Body = new MemoryStream();
            var code = "authcode";
            var formParams = new Dictionary<string, string>
            {
                { TokenEndpointConstant.Code, code },
                { TokenEndpointConstant.ClientId, this.clientId },
                { TokenEndpointConstant.GrantType, "authorization_code" },
                { TokenEndpointConstant.RedirectUri, this.redirectUri },
            };
            context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            this.providerOptions.AuthorizationCodePkceRequired = false;
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton(this.mockCodeStore.Object);
            serviceCollection.AddSingleton(this.mockUserStore.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                GrantTypes = [GrantType.AuthorizationCode],
                IdentityTokenExpiry = TimeSpan.FromMinutes(60),
            };
            this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
            var issuer = $"{this.urlScheme}://{this.host}";
            var scope = issueRefreshToken ? this.scopeOfflineAccess : this.scope1;
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                scope
            )
            {
                SessionExpiryUtc = this.utcNow.AddHours(1),
                SessionState = this.sessionState,
            };
            var content = JsonSerializer.Serialize(authorizationRequest, JsonHelper.JsonSerializerOptions);
            var storedCode = new StoredCode(
                HashHelper.GenerateCode(),
                CodeType.AuthorizationCode,
                content,
                issuer,
                this.subject
            )
            {
                Created = this.utcNow,
                Expiry = this.utcNow.AddMinutes(5),
            };
            this.mockCodeStore.Setup(m => m.ConsumeCode(code, CodeType.AuthorizationCode, issuer))
                .Returns(Task.CompletedTask);
            this.mockCodeStore.Setup(m => m.GetCode(code, CodeType.AuthorizationCode, issuer)).ReturnsAsync(storedCode);
            var storedCodeCaptured = (StoredCode?)null;
            if (issueRefreshToken)
            {
                this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>()))
                    .Callback((StoredCode storedCodePassed) => storedCodeCaptured = storedCodePassed)
                    .Returns(Task.CompletedTask);
            }
            this.mockUserStore.Setup(m => m.IsUserActive(issuer, this.subject)).ReturnsAsync(true);
            var accessToken = "access.token";
            this.mockJsonWebTokenHandler.Setup(m =>
                    m.GetAccessToken(
                        this.clientId,
                        this.utcNow + oidcClient.AccessTokenExpiry,
                        issuer,
                        It.IsAny<List<string>>(),
                        this.subject
                    )
                )
                .ReturnsAsync(accessToken);
            var idToken = "id.token";
            this.mockJsonWebTokenHandler.Setup(m =>
                    m.GetIdToken(
                        It.IsAny<AuthorizationRequest>(),
                        this.clientId,
                        issuer,
                        It.IsAny<List<string>>(),
                        this.subject
                    )
                )
                .ReturnsAsync(idToken);
            var tokenHandler = new TokenHandler(
                this.mockClientStore.Object,
                this.mockJsonWebTokenHandler.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act
            var result = await tokenHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
            var body = TestHelper.ReadBodyAsString(context.Response);
            Assert.IsNotNull(body);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(body);
            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(accessToken, tokenResponse.AccessToken);
            Assert.AreEqual(oidcClient.AccessTokenExpiry.TotalSeconds, tokenResponse.ExpiresIn);
            Assert.AreEqual(idToken, tokenResponse.IdToken);
            Assert.AreEqual(JsonWebTokenConstant.Bearer, tokenResponse.TokenType);
            if (issueRefreshToken)
            {
                Assert.IsNotNull(storedCodeCaptured);
                Assert.AreEqual(storedCodeCaptured.Code, tokenResponse.RefreshToken);
            }
            Assert.AreEqual(this.sessionState, tokenResponse.SessionState);
            this.mockClientStore.VerifyAll();
            this.mockCodeStore.VerifyAll();
            this.mockUserStore.VerifyAll();
            this.mockJsonWebTokenHandler.VerifyAll();
        }

        public enum AuthCliEx
        {
            InvalidClient,
            None,
            NoClient,
            ValidMissingGrant,
            ValidNoGrants,
        }

        public enum AuthCdeEx
        {
            Consumed,
            Expired,
            Missing,
            None,
            NotFound,
            Rubbish,
            SessionExpiryMissing,
            UserInactive,
        }

        public enum AuthReqEx
        {
            CodeChalMissing,
            CodeChalInvalid,
            CodeMethodInvalid,
            CodeRequiredMissing,
            NoContent,
            None,
            RedirectUriBad,
            RedirectUriMissing,
        }

        [DataTestMethod]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.NoClient,
            AuthReqEx.None,
            ProviderConstant.InvalidClient,
            "Token request missing client_id"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.InvalidClient,
            AuthReqEx.None,
            ProviderConstant.InvalidClient,
            "Unknown client id: {clientId}"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.ValidNoGrants,
            AuthReqEx.None,
            ProviderConstant.InvalidRequest,
            "Client does not allow authorization_code grant type"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.ValidMissingGrant,
            AuthReqEx.None,
            ProviderConstant.InvalidClient,
            "Grant types not specified for client id: {clientId}"
        )]
        [DataRow(
            AuthCdeEx.Missing,
            AuthCliEx.None,
            AuthReqEx.None,
            ProviderConstant.InvalidRequest,
            "Token request missing code"
        )]
        [DataRow(
            AuthCdeEx.Rubbish,
            AuthCliEx.None,
            AuthReqEx.None,
            ProviderConstant.InvalidRequest,
            "Token request includes invalid form field: {formFields}"
        )]
        [DataRow(
            AuthCdeEx.NotFound,
            AuthCliEx.None,
            AuthReqEx.None,
            ProviderConstant.InvalidRequest,
            "Authorisation code not found, expired, or already consumed"
        )]
        [DataRow(
            AuthCdeEx.Expired,
            AuthCliEx.None,
            AuthReqEx.None,
            ProviderConstant.InvalidRequest,
            "Authorisation code not found, expired, or already consumed"
        )]
        [DataRow(
            AuthCdeEx.Consumed,
            AuthCliEx.None,
            AuthReqEx.None,
            ProviderConstant.InvalidGrant,
            "Authorisation code not found, expired, or already consumed"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.None,
            AuthReqEx.NoContent,
            ProviderConstant.InvalidRequest,
            "Unable to deserialize persisted authorisation request"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.None,
            AuthReqEx.RedirectUriMissing,
            ProviderConstant.InvalidRequest,
            "Token request missing redirect_uri name"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.None,
            AuthReqEx.RedirectUriBad,
            ProviderConstant.InvalidRequest,
            "Persisted request parameters mismatch"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.None,
            AuthReqEx.CodeRequiredMissing,
            ProviderConstant.InvalidRequest,
            "Token request missing code_verifier"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.None,
            AuthReqEx.CodeMethodInvalid,
            ProviderConstant.InvalidRequest,
            "Invalid code challenge method {codeChallengeMethod}"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.None,
            AuthReqEx.CodeChalMissing,
            ProviderConstant.InvalidRequest,
            "Code verifier mismatch"
        )]
        [DataRow(
            AuthCdeEx.None,
            AuthCliEx.None,
            AuthReqEx.CodeChalInvalid,
            ProviderConstant.InvalidRequest,
            "Code verifier mismatch"
        )]
        [DataRow(
            AuthCdeEx.UserInactive,
            AuthCliEx.None,
            AuthReqEx.None,
            ProviderConstant.InvalidRequest,
            "User is now inactive"
        )]
        [DataRow(
            AuthCdeEx.SessionExpiryMissing,
            AuthCliEx.None,
            AuthReqEx.None,
            ProviderConstant.InvalidRequest,
            "AuthorizationRequest has no value for SessionExpiryUtc"
        )]
        public async Task HandleRequest_AuthorizationCodeExceptions(
            AuthCdeEx authCodeEx,
            AuthCliEx authClientEx,
            AuthReqEx authRequestEx,
            string expectedError,
            string expectedExceptionMessage
        )
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "POST";
            context.Request.Scheme = this.urlScheme;
            var code = "authcode";
            var issuer = $"{this.urlScheme}://{this.host}";
            var serviceCollection = new TestServiceCollection();
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            this.mockCodeStore.Setup(m => m.ConsumeCode(code, CodeType.AuthorizationCode, issuer))
                .Returns(Task.CompletedTask);
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                this.scope1
            )
            {
                SessionExpiryUtc = authCodeEx == AuthCdeEx.SessionExpiryMissing ? null : this.utcNow.AddHours(1),
            };
            var formParams = new Dictionary<string, string>
            {
                { TokenEndpointConstant.GrantType, TokenEndpointConstant.AuthorizationCode },
            };
            switch (authRequestEx)
            {
                case AuthReqEx.CodeChalMissing:
                    formParams.Add(TokenEndpointConstant.CodeVerifier, this.codeVerifier);
                    formParams.Add(TokenEndpointConstant.RedirectUri, this.redirectUri);
                    authorizationRequest.CodeChallengeMethod = CodeChallengeMethod.S256;
                    authorizationRequest.RedirectUri = this.redirectUri;
                    break;
                case AuthReqEx.CodeChalInvalid:
                    formParams.Add(TokenEndpointConstant.CodeVerifier, "rubbish");
                    formParams.Add(TokenEndpointConstant.RedirectUri, this.redirectUri);
                    authorizationRequest.CodeChallengeMethod = CodeChallengeMethod.S256;
                    authorizationRequest.RedirectUri = this.redirectUri;
                    break;
                case AuthReqEx.CodeMethodInvalid:
                    formParams.Add(TokenEndpointConstant.CodeVerifier, this.codeVerifier);
                    formParams.Add(TokenEndpointConstant.RedirectUri, this.redirectUri);
                    authorizationRequest.CodeChallengeMethod = CodeChallengeMethod.None;
                    authorizationRequest.RedirectUri = this.redirectUri;
                    break;
                case AuthReqEx.CodeRequiredMissing:
                    formParams.Add(TokenEndpointConstant.RedirectUri, this.redirectUri);
                    break;
                case AuthReqEx.NoContent:
                    authorizationRequest = null;
                    break;
                case AuthReqEx.None:
                    formParams.Add(TokenEndpointConstant.CodeVerifier, this.codeVerifier);
                    formParams.Add(TokenEndpointConstant.RedirectUri, this.redirectUri);
                    authorizationRequest.CodeChallenge = this.codeChallenge;
                    authorizationRequest.CodeChallengeMethod = CodeChallengeMethod.S256;
                    authorizationRequest.RedirectUri = this.redirectUri;
                    this.providerOptions.AuthorizationCodePkceRequired = false;
                    break;
                case AuthReqEx.RedirectUriBad:
                    formParams.Add(TokenEndpointConstant.RedirectUri, this.redirectUri);
                    authorizationRequest.RedirectUri = "https://dodgy.app/auth/callback";
                    break;
                case AuthReqEx.RedirectUriMissing:
                    break;
            }
            var content = JsonSerializer.Serialize(authorizationRequest, JsonHelper.JsonSerializerOptions);
            var storedCode = new StoredCode(
                HashHelper.GenerateCode(),
                CodeType.AuthorizationCode,
                content,
                issuer,
                this.subject
            )
            {
                ConsumeCount = authCodeEx == AuthCdeEx.Consumed ? 1 : 0,
                Created = this.utcNow,
                Expiry = this.utcNow.AddMinutes(authCodeEx == AuthCdeEx.Expired ? -5 : 5),
            };
            this.mockUserStore.Setup(m => m.IsUserActive(issuer, storedCode.Subject))
                .ReturnsAsync(authCodeEx != AuthCdeEx.UserInactive);
            serviceCollection.AddSingleton(this.mockCodeStore.Object);
            serviceCollection.AddSingleton(this.mockUserStore.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var tokenHandler = new TokenHandler(
                this.mockClientStore.Object,
                this.mockJsonWebTokenHandler.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            switch (authCodeEx)
            {
                case AuthCdeEx.Consumed:
                case AuthCdeEx.Expired:
                case AuthCdeEx.None:
                case AuthCdeEx.SessionExpiryMissing:
                case AuthCdeEx.UserInactive:
                    formParams.Add(TokenEndpointConstant.Code, code);
                    this.mockCodeStore.Setup(m => m.GetCode(code, CodeType.AuthorizationCode, issuer))
                        .ReturnsAsync(storedCode);
                    break;
                case AuthCdeEx.NotFound:
                    formParams.Add(TokenEndpointConstant.Code, code);
                    this.mockCodeStore.Setup(m => m.GetCode(code, CodeType.AuthorizationCode, issuer))
                        .ReturnsAsync((StoredCode?)null);
                    break;
                case AuthCdeEx.Rubbish:
                    formParams.Add(TokenEndpointConstant.Code, code);
                    formParams.Add("rubbish", "should_fail");
                    break;
            }
            if (authCodeEx == AuthCdeEx.None)
            {
                this.mockCodeStore.Setup(m => m.ConsumeCode(code, CodeType.AuthorizationCode, issuer))
                    .Returns(Task.CompletedTask);
            }
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                GrantTypes = authClientEx switch
                {
                    AuthCliEx.ValidMissingGrant => null,
                    AuthCliEx.ValidNoGrants => [],
                    _ => [GrantType.AuthorizationCode]
                },
                IdentityTokenExpiry = TimeSpan.FromMinutes(60),
            };
            switch (authClientEx)
            {
                case AuthCliEx.InvalidClient:
                    this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync((OidcClient?)null);
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    break;
                case AuthCliEx.NoClient:
                    break;
                case AuthCliEx.None:
                case AuthCliEx.ValidMissingGrant:
                case AuthCliEx.ValidNoGrants:
                    this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    break;
            }
            context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            // Act
            var exception = await Assert.ThrowsExceptionAsync<BadRequestException>(
                async () => await tokenHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
            Assert.AreEqual(expectedError, exception.Error);
        }

        public enum ClientSecretType
        {
            BothAuthorization,
            BothQueryParams,
            Mixed,
        }

        [DataTestMethod]
        [DataRow(ClientSecretType.BothAuthorization)]
        [DataRow(ClientSecretType.BothQueryParams)]
        [DataRow(ClientSecretType.Mixed)]
        public async Task HandleRequest_ClientCredentials(ClientSecretType clientSecretType)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "POST";
            context.Request.Scheme = this.urlScheme;
            context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            context.Response.Body = new MemoryStream();
            var formParams = new Dictionary<string, string>
            {
                { TokenEndpointConstant.GrantType, "client_credentials" },
                { TokenEndpointConstant.Scope, this.scope1 },
            };
            var headerRaw = $"{Uri.EscapeDataString(this.clientId)}:{Uri.EscapeDataString(this.clientSecret)}";
            var headerEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerRaw));
            switch (clientSecretType)
            {
                case ClientSecretType.BothAuthorization:
                    context.Request.Headers.Authorization = $"{ProviderConstant.Basic}{headerEncoded}";
                    break;
                case ClientSecretType.BothQueryParams:
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    formParams.Add(TokenEndpointConstant.ClientSecret, this.clientSecret);
                    break;
                case ClientSecretType.Mixed:
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    context.Request.Headers.Authorization = $"{ProviderConstant.Basic}{headerEncoded}";
                    break;
            }
            context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            this.providerOptions.GrantTypes.Add(GrantType.ClientCredentials);
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                ClientSecretRequired = true,
                GrantTypes = [GrantType.ClientCredentials],
                Scopes = [this.scope1],
            };
            this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
            this.mockClientStore.Setup(m => m.IsCorrectClientSecret(this.clientId, this.clientSecret))
                .ReturnsAsync(true);
            var issuer = $"{this.urlScheme}://{this.host}";
            var accessToken = "access.token";
            this.mockJsonWebTokenHandler.Setup(m =>
                    m.GetAccessToken(
                        this.clientId,
                        this.utcNow + oidcClient.AccessTokenExpiry,
                        issuer,
                        It.IsAny<List<string>>(),
                        null
                    )
                )
                .ReturnsAsync(accessToken);
            var tokenHandler = new TokenHandler(
                this.mockClientStore.Object,
                this.mockJsonWebTokenHandler.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act
            var result = await tokenHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
            var body = TestHelper.ReadBodyAsString(context.Response);
            Assert.IsNotNull(body);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(body);
            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(accessToken, tokenResponse.AccessToken);
            Assert.AreEqual(oidcClient.AccessTokenExpiry.TotalSeconds, tokenResponse.ExpiresIn);
            Assert.AreEqual(JsonWebTokenConstant.Bearer, tokenResponse.TokenType);
            this.mockClientStore.VerifyAll();
            this.mockJsonWebTokenHandler.VerifyAll();
        }

        public enum CredAutEx
        {
            BadPrefix,
            BadParts,
            BadPartOne,
            BadPartTwo,
            BadPartThree,
            ClientMismatch,
            None,
        }

        public enum CredCliEx
        {
            BadScope,
            BadSecret,
            MissingGrant,
            NoGrants,
            NoScopes,
            None,
        }

        public enum CredPrmEx
        {
            InvalidParam,
            None,
            NoScope,
            NoSecret,
            Nothing,
        }

        [DataTestMethod]
        [DataRow(
            CredAutEx.None,
            CredCliEx.None,
            CredPrmEx.InvalidParam,
            ProviderConstant.InvalidRequest,
            "Token request includes invalid form field: {formFields}"
        )]
        [DataRow(
            CredAutEx.BadPrefix,
            CredCliEx.None,
            CredPrmEx.Nothing,
            ProviderConstant.InvalidClient,
            "Invalid client secret Authorization header"
        )]
        [DataRow(
            CredAutEx.BadParts,
            CredCliEx.None,
            CredPrmEx.Nothing,
            ProviderConstant.InvalidClient,
            "Malformed client secret Authorization header"
        )]
        [DataRow(
            CredAutEx.BadPartOne,
            CredCliEx.None,
            CredPrmEx.Nothing,
            ProviderConstant.InvalidClient,
            "Malformed client secret Authorization header"
        )]
        [DataRow(
            CredAutEx.BadPartTwo,
            CredCliEx.None,
            CredPrmEx.Nothing,
            ProviderConstant.InvalidClient,
            "Malformed client secret Authorization header"
        )]
        [DataRow(
            CredAutEx.BadPartThree,
            CredCliEx.None,
            CredPrmEx.Nothing,
            ProviderConstant.InvalidClient,
            "Malformed client secret Authorization header"
        )]
        [DataRow(
            CredAutEx.ClientMismatch,
            CredCliEx.None,
            CredPrmEx.NoSecret,
            ProviderConstant.InvalidClient,
            "Client identifier in Authorization header does not match form field"
        )]
        [DataRow(
            CredAutEx.None,
            CredCliEx.None,
            CredPrmEx.NoSecret,
            ProviderConstant.InvalidClient,
            "Token request missing client_secret"
        )]
        [DataRow(
            CredAutEx.None,
            CredCliEx.BadSecret,
            CredPrmEx.None,
            ProviderConstant.InvalidClient,
            "Invalid client_secret"
        )]
        [DataRow(
            CredAutEx.None,
            CredCliEx.NoGrants,
            CredPrmEx.None,
            ProviderConstant.InvalidClient,
            "Grant types not specified for client id: {clientId}"
        )]
        [DataRow(
            CredAutEx.None,
            CredCliEx.MissingGrant,
            CredPrmEx.None,
            ProviderConstant.InvalidClient,
            "Client does not allow client_credentials grant type"
        )]
        [DataRow(
            CredAutEx.None,
            CredCliEx.NoScopes,
            CredPrmEx.None,
            ProviderConstant.InvalidClient,
            "Scopes not specified for client id: {clientId}"
        )]
        [DataRow(
            CredAutEx.None,
            CredCliEx.BadScope,
            CredPrmEx.None,
            ProviderConstant.InvalidScope,
            "Invalid scope requested: {scopes}"
        )]
        [DataRow(
            CredAutEx.None,
            CredCliEx.None,
            CredPrmEx.NoScope,
            ProviderConstant.InvalidScope,
            "Token request missing scope name"
        )]
        public async Task HandleRequest_ClientCredentialsExceptions(
            CredAutEx credAutEx,
            CredCliEx credClientEx,
            CredPrmEx credParamEx,
            string expectedError,
            string expectedExceptionMessage
        )
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "POST";
            context.Request.Scheme = this.urlScheme;
            context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            context.Response.Body = new MemoryStream();
            var headerRaw = string.Empty;
            var headerEncoded = string.Empty;
            switch (credAutEx)
            {
                case CredAutEx.BadPrefix:
                    context.Request.Headers.Authorization = "rubbish";
                    break;
                case CredAutEx.BadParts:
                    context.Request.Headers.Authorization = ProviderConstant.Basic;
                    break;
                case CredAutEx.BadPartOne:
                    headerRaw = $":{Uri.EscapeDataString(this.clientSecret)}";
                    headerEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerRaw));
                    context.Request.Headers.Authorization = $"{ProviderConstant.Basic}{headerEncoded}";
                    break;
                case CredAutEx.BadPartTwo:
                    headerRaw = $"{Uri.EscapeDataString(this.clientId)}:";
                    headerEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerRaw));
                    context.Request.Headers.Authorization = $"{ProviderConstant.Basic}{headerEncoded}";
                    break;
                case CredAutEx.BadPartThree:
                    headerRaw = $"{Uri.EscapeDataString(this.clientId)}:{Uri.EscapeDataString(this.clientSecret)}:";
                    headerEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerRaw));
                    context.Request.Headers.Authorization = $"{ProviderConstant.Basic}{headerEncoded}";
                    break;
                case CredAutEx.ClientMismatch:
                    headerRaw = $"{Uri.EscapeDataString("badclient")}:{Uri.EscapeDataString(this.clientSecret)}";
                    headerEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(headerRaw));
                    context.Request.Headers.Authorization = $"{ProviderConstant.Basic}{headerEncoded}";
                    break;
            }
            var formParams = new Dictionary<string, string>()
            {
                { TokenEndpointConstant.GrantType, "client_credentials" },
            };
            switch (credParamEx)
            {
                case CredPrmEx.InvalidParam:
                    formParams.Add("rubbish", "garbage");
                    break;
                case CredPrmEx.NoSecret:
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    break;
                case CredPrmEx.None:
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    formParams.Add(TokenEndpointConstant.ClientSecret, this.clientSecret);
                    formParams.Add(TokenEndpointConstant.Scope, this.scope1);
                    break;
                case CredPrmEx.NoScope:
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    formParams.Add(TokenEndpointConstant.ClientSecret, this.clientSecret);
                    break;
            }
            context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            this.providerOptions.GrantTypes.Add(GrantType.ClientCredentials);
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                ClientSecretRequired = true,
                GrantTypes = credClientEx switch
                {
                    CredCliEx.NoGrants => null,
                    CredCliEx.MissingGrant => [],
                    _ => [GrantType.ClientCredentials]
                },
                Scopes = credClientEx switch
                {
                    CredCliEx.NoScopes => null,
                    CredCliEx.BadScope => ["other"],
                    _ => [this.scope1]
                },
            };
            switch (credClientEx)
            {
                case CredCliEx.BadSecret:
                    this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
                    this.mockClientStore.Setup(m => m.IsCorrectClientSecret(this.clientId, this.clientSecret))
                        .ReturnsAsync(false);
                    break;
                case CredCliEx.BadScope:
                case CredCliEx.MissingGrant:
                case CredCliEx.NoGrants:
                case CredCliEx.None:
                case CredCliEx.NoScopes:
                    this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
                    this.mockClientStore.Setup(m => m.IsCorrectClientSecret(this.clientId, this.clientSecret))
                        .ReturnsAsync(true);
                    break;
            }
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var tokenHandler = new TokenHandler(
                this.mockClientStore.Object,
                this.mockJsonWebTokenHandler.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<BadRequestException>(
                async () => await tokenHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
            Assert.AreEqual(expectedError, exception.Error);
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task HandleRequest_RefreshToken(bool changeScope)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "POST";
            context.Request.Scheme = this.urlScheme;
            context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            context.Response.Body = new MemoryStream();
            var refreshToken = "refreshme";
            var formParams = new Dictionary<string, string>
            {
                { TokenEndpointConstant.ClientId, this.clientId },
                { TokenEndpointConstant.GrantType, "refresh_token" },
                { TokenEndpointConstant.RefreshToken, refreshToken },
            };
            if (changeScope)
            {
                formParams.Add(TokenEndpointConstant.Scope, this.scope2);
            }
            context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton(this.mockCodeStore.Object);
            serviceCollection.AddSingleton(this.mockUserStore.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                GrantTypes = [GrantType.RefreshToken],
                Scopes = [this.scope1, this.scope2],
            };
            this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
            var issuer = $"{this.urlScheme}://{this.host}";
            var refreshTokenRequest = new RefreshTokenRequest(
                this.clientId,
                string.Join(',', [this.scope1, this.scope2]),
                this.utcNow.AddHours(1)
            );
            var content = JsonSerializer.Serialize(refreshTokenRequest, JsonHelper.JsonSerializerOptions);
            var storedCode = new StoredCode(refreshToken, CodeType.RefreshTokenCode, content, issuer, this.subject)
            {
                Created = this.utcNow,
                Expiry = this.utcNow.AddMinutes(5),
            };
            this.mockCodeStore.Setup(m => m.GetCode(refreshToken, CodeType.RefreshTokenCode, issuer))
                .ReturnsAsync(storedCode);
            var storedCodeCaptured = (StoredCode?)null;
            this.mockCodeStore.Setup(m => m.SaveCode(It.IsAny<StoredCode>()))
                .Callback((StoredCode storedCodePassed) => storedCodeCaptured = storedCodePassed)
                .Returns(Task.CompletedTask);
            this.mockUserStore.Setup(m => m.IsUserActive(issuer, this.subject)).ReturnsAsync(true);
            var refreshedToken = "nowfresher";
            this.mockJsonWebTokenHandler.Setup(m =>
                    m.GetAccessToken(
                        this.clientId,
                        this.utcNow + oidcClient.AccessTokenExpiry,
                        issuer,
                        It.IsAny<List<string>>(),
                        this.subject
                    )
                )
                .ReturnsAsync(refreshedToken);
            this.mockCodeStore.Setup(m => m.DeleteCode(refreshToken, CodeType.RefreshTokenCode, issuer))
                .Returns(Task.CompletedTask);
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            this.providerOptions.GrantTypes.Add(GrantType.RefreshToken);
            var tokenHandler = new TokenHandler(
                this.mockClientStore.Object,
                this.mockJsonWebTokenHandler.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act
            var result = await tokenHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
            var body = TestHelper.ReadBodyAsString(context.Response);
            Assert.IsNotNull(body);
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(body);
            Assert.IsNotNull(tokenResponse);
            Assert.AreEqual(refreshedToken, tokenResponse.AccessToken);
            Assert.AreEqual(oidcClient.AccessTokenExpiry.TotalSeconds, tokenResponse.ExpiresIn);
            Assert.AreEqual(JsonWebTokenConstant.Bearer, tokenResponse.TokenType);
            this.mockClientStore.VerifyAll();
            this.mockCodeStore.VerifyAll();
            this.mockJsonWebTokenHandler.VerifyAll();
            this.mockUserStore.VerifyAll();
        }

        public enum RefrCdeEx
        {
            BadScope,
            ClientMismatch,
            Expired,
            NoContent,
            NotFound,
            Nothing,
            None,
            UserInactive,
        }

        public enum RefrCliEx
        {
            InvalidClient,
            MissingGrant,
            NoGrants,
            Nothing,
            None,
        }

        public enum RefrPrmEx
        {
            InvalidParam,
            NoClient,
            None,
            NoRefreshToken,
        }

        [DataTestMethod]
        [DataRow(
            RefrCdeEx.None,
            RefrCliEx.Nothing,
            RefrPrmEx.NoRefreshToken,
            ProviderConstant.InvalidRequest,
            "Token request missing refresh token"
        )]
        [DataRow(
            RefrCdeEx.None,
            RefrCliEx.Nothing,
            RefrPrmEx.InvalidParam,
            ProviderConstant.InvalidRequest,
            "Token request includes invalid form field: {formFields}"
        )]
        [DataRow(
            RefrCdeEx.None,
            RefrCliEx.Nothing,
            RefrPrmEx.NoClient,
            ProviderConstant.InvalidClient,
            "Token request missing client_id"
        )]
        [DataRow(
            RefrCdeEx.None,
            RefrCliEx.InvalidClient,
            RefrPrmEx.None,
            ProviderConstant.InvalidClient,
            "Unknown client id: {clientId}"
        )]
        [DataRow(
            RefrCdeEx.None,
            RefrCliEx.NoGrants,
            RefrPrmEx.None,
            ProviderConstant.InvalidClient,
            "Grant types not specified for client id: {clientId}"
        )]
        [DataRow(
            RefrCdeEx.None,
            RefrCliEx.MissingGrant,
            RefrPrmEx.None,
            ProviderConstant.InvalidRequest,
            "Client does not allow refresh_token grant type"
        )]
        [DataRow(
            RefrCdeEx.NotFound,
            RefrCliEx.None,
            RefrPrmEx.None,
            ProviderConstant.InvalidRequest,
            "Refresh token not found or expired"
        )]
        [DataRow(
            RefrCdeEx.Expired,
            RefrCliEx.None,
            RefrPrmEx.None,
            ProviderConstant.InvalidRequest,
            "Refresh token not found or expired"
        )]
        [DataRow(
            RefrCdeEx.NoContent,
            RefrCliEx.None,
            RefrPrmEx.None,
            ProviderConstant.InvalidRequest,
            "Unable to deserialize persisted refresh token request"
        )]
        [DataRow(
            RefrCdeEx.ClientMismatch,
            RefrCliEx.None,
            RefrPrmEx.None,
            ProviderConstant.InvalidRequest,
            "Client does not match refresh token request"
        )]
        [DataRow(
            RefrCdeEx.UserInactive,
            RefrCliEx.None,
            RefrPrmEx.None,
            ProviderConstant.InvalidToken,
            "Refresh token user inactive"
        )]
        [DataRow(
            RefrCdeEx.BadScope,
            RefrCliEx.None,
            RefrPrmEx.None,
            ProviderConstant.InvalidScope,
            "Invalid scope requested: {scopes}"
        )]
        public async Task HandleRequest_RefreshTokenExceptions(
            RefrCdeEx refrCodeEx,
            RefrCliEx refrClientEx,
            RefrPrmEx refrParamEx,
            string expectedError,
            string expectedExceptionMessage
        )
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "POST";
            context.Request.Scheme = this.urlScheme;
            context.Request.ContentType = ContentTypeConstant.ApplicationForm;
            context.Response.Body = new MemoryStream();
            var formParams = new Dictionary<string, string> { { TokenEndpointConstant.GrantType, "refresh_token" } };
            var refreshToken = "refreshme";
            switch (refrParamEx)
            {
                case RefrPrmEx.InvalidParam:
                    formParams.Add(TokenEndpointConstant.RefreshToken, refreshToken);
                    formParams.Add("rubbish", "garbage");
                    break;
                case RefrPrmEx.NoClient:
                    formParams.Add(TokenEndpointConstant.RefreshToken, refreshToken);
                    break;
                case RefrPrmEx.None:
                    formParams.Add(TokenEndpointConstant.ClientId, this.clientId);
                    formParams.Add(TokenEndpointConstant.RefreshToken, refreshToken);
                    formParams.Add(TokenEndpointConstant.Scope, this.scope1);
                    break;
            }
            context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton(this.mockCodeStore.Object);
            serviceCollection.AddSingleton(this.mockUserStore.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            this.providerOptions.GrantTypes.Add(GrantType.RefreshToken);
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                GrantTypes = refrClientEx switch
                {
                    RefrCliEx.NoGrants => null,
                    RefrCliEx.MissingGrant => [],
                    _ => [GrantType.RefreshToken]
                },
                Scopes = [this.scope1],
            };
            var issuer = $"{this.urlScheme}://{this.host}";
            switch (refrClientEx)
            {
                case RefrCliEx.InvalidClient:
                    this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync((OidcClient?)null);
                    break;
                case RefrCliEx.MissingGrant:
                case RefrCliEx.NoGrants:
                case RefrCliEx.None:
                    this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
                    break;
            }
            var refreshTokenRequest =
                refrCodeEx == RefrCdeEx.NoContent
                    ? null
                    : new RefreshTokenRequest(
                        refrCodeEx == RefrCdeEx.ClientMismatch ? "badclient" : this.clientId,
                        refrCodeEx == RefrCdeEx.BadScope ? "badscope" : this.scope1,
                        this.utcNow.AddHours(1)
                    );
            var content = JsonSerializer.Serialize(refreshTokenRequest, JsonHelper.JsonSerializerOptions);
            var storedCode = new StoredCode(refreshToken, CodeType.RefreshTokenCode, content, issuer, this.subject)
            {
                Created = this.utcNow,
                Expiry = this.utcNow.AddMinutes(refrCodeEx == RefrCdeEx.Expired ? -5 : 5),
            };
            switch (refrCodeEx)
            {
                case RefrCdeEx.Expired:
                    this.mockCodeStore.Setup(m => m.DeleteCode(refreshToken, CodeType.RefreshTokenCode, issuer))
                        .Returns(Task.CompletedTask);
                    this.mockCodeStore.Setup(m => m.GetCode(refreshToken, CodeType.RefreshTokenCode, issuer))
                        .ReturnsAsync(storedCode);
                    break;
                case RefrCdeEx.NoContent:
                case RefrCdeEx.ClientMismatch:
                    this.mockCodeStore.Setup(m => m.GetCode(refreshToken, CodeType.RefreshTokenCode, issuer))
                        .ReturnsAsync(storedCode);
                    break;
                case RefrCdeEx.BadScope:
                case RefrCdeEx.UserInactive:
                case RefrCdeEx.Nothing:
                    this.mockCodeStore.Setup(m => m.GetCode(refreshToken, CodeType.RefreshTokenCode, issuer))
                        .ReturnsAsync(storedCode);
                    this.mockUserStore.Setup(m => m.IsUserActive(issuer, storedCode.Subject))
                        .ReturnsAsync(refrCodeEx != RefrCdeEx.UserInactive);
                    break;
                case RefrCdeEx.NotFound:
                    this.mockCodeStore.Setup(m => m.GetCode(refreshToken, CodeType.RefreshTokenCode, issuer))
                        .ReturnsAsync((StoredCode?)null);
                    break;
            }
            var tokenHandler = new TokenHandler(
                this.mockClientStore.Object,
                this.mockJsonWebTokenHandler.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<BadRequestException>(
                async () => await tokenHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
            Assert.AreEqual(expectedError, exception.Error);
            this.mockClientStore.VerifyAll();
            this.mockCodeStore.VerifyAll();
            this.mockJsonWebTokenHandler.VerifyAll();
            this.mockUserStore.VerifyAll();
        }

        /*private class TokenResponse
        {
            [JsonPropertyName(JsonWebTokenConstant.AccessToken)]
            public string? AccessToken { get; set; }

            [JsonPropertyName(JsonWebTokenConstant.ExpiresIn)]
            public double? ExpiresIn { get; set; }

            [JsonPropertyName(JsonWebTokenConstant.IdToken)]
            public string? IdToken { get; set; }

            [JsonPropertyName(JsonWebTokenConstant.RefreshToken)]
            public string? RefreshToken { get; set; }

            [JsonPropertyName(JsonWebTokenConstant.SessionState)]
            public string? SessionState { get; set; }

            [JsonPropertyName(JsonWebTokenConstant.TokenType)]
            public string? TokenType { get; set; }
        }*/
    }
}
