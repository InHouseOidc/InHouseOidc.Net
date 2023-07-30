// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class ValidationHandlerTest
    {
        private readonly TestLogger<ValidationHandler> logger = new();
        private readonly string clientId = "client";
        private readonly string codeChallenge = "wv95kX5SYAG4L3CRoJuiSfSYfcf1jM6dUwjn57Ily4I";
        private readonly string idTokenHint = "hint.hint";
        private readonly int maxAge = 1000;
        private readonly string nonce = "only.the.once";
        private readonly string redirectUri = "https://client.app/auth/callback";
        private readonly string scope1 = "scope1";
        private readonly string state = "somevalue";

        private Mock<IClientStore> mockClientStore = new(MockBehavior.Strict);
        private Mock<ISigningKeyHandler> mockSigningKeyHandler = new(MockBehavior.Strict);
        private Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private ProviderOptions providerOptions = new();
        private ServiceProvider serviceProvider = new TestServiceCollection().BuildServiceProvider();

        [TestInitialize]
        public void Initialise()
        {
            this.mockClientStore = new Mock<IClientStore>(MockBehavior.Strict);
            this.logger.Clear();
            this.providerOptions = new();
        }

        [TestMethod]
        public async Task ParseValidateAuthorizationRequest()
        {
            // Arrange
            var handler = new ValidationHandler(
                this.mockClientStore.Object,
                this.logger,
                this.providerOptions,
                this.mockSigningKeyHandler.Object,
                this.mockUtcNow.Object
            );
            var requestScope = $"openid {this.scope1}";
            var parameters = new Dictionary<string, string>
            {
                { AuthorizationEndpointConstant.ClientId, this.clientId },
                { AuthorizationEndpointConstant.IdTokenHint, this.idTokenHint },
                { AuthorizationEndpointConstant.MaxAge, this.maxAge.ToString() },
                { AuthorizationEndpointConstant.Nonce, this.nonce },
                { AuthorizationEndpointConstant.Prompt, DiscoveryConstant.PromptLogin },
                { AuthorizationEndpointConstant.RedirectUri, this.redirectUri },
                { AuthorizationEndpointConstant.ResponseMode, DiscoveryConstant.ResponseModeQuery },
                { AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code },
                { AuthorizationEndpointConstant.Scope, requestScope },
                { AuthorizationEndpointConstant.State, this.state },
            };
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                GrantTypes = new() { GrantType.AuthorizationCode },
                IdentityTokenExpiry = TimeSpan.FromMinutes(60),
                RedirectUris = new List<string> { this.redirectUri },
                Scopes = new List<string> { "openid", this.scope1 },
            };
            this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
            this.providerOptions.AuthorizationCodePkceRequired = false;
            // Act
            var (authorizationRequest, redirectError) = await handler.ParseValidateAuthorizationRequest(parameters);
            // Assert
            Assert.IsNotNull(authorizationRequest);
            Assert.IsNull(redirectError);
            Assert.AreEqual(this.clientId, authorizationRequest.ClientId);
            Assert.AreEqual(this.idTokenHint, authorizationRequest.IdTokenHint);
            Assert.AreEqual(this.maxAge, authorizationRequest.MaxAge);
            Assert.AreEqual(this.nonce, authorizationRequest.Nonce);
            Assert.AreEqual(Prompt.Login, authorizationRequest.Prompt);
            Assert.AreEqual(this.redirectUri, authorizationRequest.RedirectUri);
            Assert.AreEqual(ResponseMode.Query, authorizationRequest.ResponseMode);
            Assert.AreEqual(ResponseType.Code, authorizationRequest.ResponseType);
            Assert.AreEqual(requestScope, authorizationRequest.Scope);
            Assert.AreEqual(this.state, authorizationRequest.State);
        }

        public enum ValCliEx
        {
            GrantMissing,
            GrantsNull,
            InvalidClient,
            NoClient,
            None,
            RedirectUriEmpty,
            RedirectUriBad,
            ScopeInvalid,
            ScopesNull,
        }

        public enum ValReqEx
        {
            CodeChallengeMethodBad,
            CodeChallengeShort,
            CodeChallengeLong,
            MaxAgeBad,
            NonceLength,
            None,
            Nothing,
            ParamMissingNonPkce,
            ParamMissingPkce,
            PromptInvalid,
            RedirectUriMissing,
            RedirectUriLength,
            RedirectUriBad,
            RequestNotSupported,
            ResponseModeInvalid,
            ResponseTypeInvalid,
            ScopeLength,
            ScopeNoOpenId,
            StateLength,
        }

        [DataTestMethod]
        [DataRow(
            ValCliEx.NoClient,
            ValReqEx.Nothing,
            ProviderConstant.InvalidRequest,
            "Authorization request missing client_id"
        )]
        [DataRow(
            ValCliEx.InvalidClient,
            ValReqEx.Nothing,
            ProviderConstant.InvalidRequest,
            "Unknown client id: {clientId}"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.RedirectUriMissing,
            ProviderConstant.InvalidRequest,
            "Authorization request missing redirect_uri"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.RedirectUriLength,
            ProviderConstant.InvalidRequest,
            "Redirect URI exceeds maximum length of 512 characters"
        )]
        [DataRow(
            ValCliEx.RedirectUriBad,
            ValReqEx.None,
            ProviderConstant.InvalidRequest,
            "Authorization redirect_uri invalid: {redirectUri}"
        )]
        [DataRow(
            ValCliEx.RedirectUriEmpty,
            ValReqEx.None,
            ProviderConstant.InvalidRequest,
            "Redirect URIs not specified for client id: {clientId}"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.StateLength,
            ProviderConstant.InvalidRequest,
            "State exceeds maximum length of 512 characters"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.RequestNotSupported,
            ProviderConstant.RequestNotSupported,
            "Request parameter is not supported"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.ParamMissingNonPkce,
            ProviderConstant.InvalidRequest,
            "Authorization missing one or more required fields: {requiredFields}"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.ParamMissingPkce,
            ProviderConstant.InvalidRequest,
            "Authorization missing one or more required fields: {requiredFields}"
        )]
        [DataRow(
            ValCliEx.GrantsNull,
            ValReqEx.None,
            ProviderConstant.UnauthorizedClient,
            "Grant types not specified for client id: {clientId}"
        )]
        [DataRow(
            ValCliEx.GrantMissing,
            ValReqEx.None,
            ProviderConstant.UnauthorizedClient,
            "Client does not allow authorization_code grant type"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.ResponseTypeInvalid,
            ProviderConstant.InvalidRequest,
            "Invalid response type: {responseType}"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.CodeChallengeMethodBad,
            ProviderConstant.InvalidRequest,
            "Invalid code challenge method: {codeChallengeMethod}"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.CodeChallengeShort,
            ProviderConstant.InvalidRequest,
            "Invalid code challenge length. Expected 43-128 characters"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.CodeChallengeLong,
            ProviderConstant.InvalidRequest,
            "Invalid code challenge length. Expected 43-128 characters"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.ScopeLength,
            ProviderConstant.InvalidRequest,
            "Scope exceeds maximum length of 512 characters"
        )]
        [DataRow(
            ValCliEx.None,
            ValReqEx.ScopeNoOpenId,
            ProviderConstant.InvalidScope,
            "Scope missing required openid entry"
        )]
        [DataRow(
            ValCliEx.ScopesNull,
            ValReqEx.None,
            ProviderConstant.InvalidScope,
            "Scopes not specified for client id: {clientId}"
        )]
        [DataRow(
            ValCliEx.ScopeInvalid,
            ValReqEx.None,
            ProviderConstant.InvalidScope,
            "Invalid scope requested: {scopes}"
        )]
        [DataRow(ValCliEx.None, ValReqEx.MaxAgeBad, ProviderConstant.InvalidRequest, "Invalid max age: {maxAge}")]
        [DataRow(
            ValCliEx.None,
            ValReqEx.NonceLength,
            ProviderConstant.InvalidRequest,
            "Nonce exceeds maximum length of 512 characters"
        )]
        [DataRow(ValCliEx.None, ValReqEx.PromptInvalid, ProviderConstant.InvalidRequest, "Invalid prompt: {prompt}")]
        [DataRow(
            ValCliEx.None,
            ValReqEx.ResponseModeInvalid,
            ProviderConstant.InvalidRequest,
            "Invalid response mode: {responseMode}"
        )]
        public async Task ParseValidateAuthorizationRequest_Exceptions(
            ValCliEx valCliEx,
            ValReqEx valReqEx,
            string expectedError,
            string expectedExceptionMessage
        )
        {
            // Arrange
            this.providerOptions.AuthorizationCodePkceRequired = false;
            var handler = new ValidationHandler(
                this.mockClientStore.Object,
                this.logger,
                this.providerOptions,
                this.mockSigningKeyHandler.Object,
                this.mockUtcNow.Object
            );
            var parameters = new Dictionary<string, string>();
            var oidcClient = new OidcClient
            {
                AccessTokenExpiry = TimeSpan.FromMinutes(15),
                ClientId = this.clientId,
                GrantTypes = valCliEx switch
                {
                    ValCliEx.GrantMissing => new(),
                    ValCliEx.GrantsNull => null,
                    _ => new() { GrantType.AuthorizationCode }
                },
                IdentityTokenExpiry = TimeSpan.FromMinutes(60),
                RedirectUris = valCliEx switch
                {
                    ValCliEx.RedirectUriBad => new List<string>(),
                    ValCliEx.RedirectUriEmpty => null,
                    _ => new List<string> { this.redirectUri }
                },
                Scopes = valCliEx switch
                {
                    ValCliEx.ScopeInvalid => new(),
                    ValCliEx.ScopesNull => null,
                    _ => new List<string> { "openid", this.scope1 }
                },
            };
            switch (valReqEx)
            {
                case ValReqEx.CodeChallengeMethodBad:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.CodeChallengeMethod, "rubbish");
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, this.scope1);
                    break;
                case ValReqEx.CodeChallengeShort:
                case ValReqEx.CodeChallengeLong:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(
                        AuthorizationEndpointConstant.CodeChallengeMethod,
                        AuthorizationEndpointConstant.S256
                    );
                    parameters.Add(
                        AuthorizationEndpointConstant.CodeChallenge,
                        valReqEx == ValReqEx.CodeChallengeShort ? "too_short_to_be_real" : new string('c', 129)
                    );
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, this.scope1);
                    break;
                case ValReqEx.MaxAgeBad:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.MaxAge, "bad");
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, $"{JsonWebTokenConstant.OpenId} {this.scope1}");
                    break;
                case ValReqEx.NonceLength:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.Nonce, new string('x', 513));
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, $"{JsonWebTokenConstant.OpenId} {this.scope1}");
                    break;
                case ValReqEx.None:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, $"{JsonWebTokenConstant.OpenId} {this.scope1}");
                    break;
                case ValReqEx.ParamMissingNonPkce:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    break;
                case ValReqEx.ParamMissingPkce:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    this.providerOptions.AuthorizationCodePkceRequired = true;
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    break;
                case ValReqEx.PromptInvalid:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.Prompt, "invalid");
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, $"{JsonWebTokenConstant.OpenId} {this.scope1}");
                    break;
                case ValReqEx.RedirectUriBad:
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, "https://dodgy.site");
                    break;
                case ValReqEx.RedirectUriLength:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, new string('x', 513));
                    break;
                case ValReqEx.RedirectUriMissing:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    break;
                case ValReqEx.RequestNotSupported:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.Request, "nice_try");
                    break;
                case ValReqEx.ResponseModeInvalid:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.ResponseMode, "invalid");
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, $"{JsonWebTokenConstant.OpenId} {this.scope1}");
                    break;
                case ValReqEx.ResponseTypeInvalid:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, "rubbish");
                    parameters.Add(AuthorizationEndpointConstant.Scope, this.scope1);
                    break;
                case ValReqEx.ScopeLength:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.CodeChallenge, this.codeChallenge);
                    parameters.Add(
                        AuthorizationEndpointConstant.CodeChallengeMethod,
                        AuthorizationEndpointConstant.S256
                    );
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, new string('s', 513));
                    break;
                case ValReqEx.ScopeNoOpenId:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.CodeChallenge, this.codeChallenge);
                    parameters.Add(
                        AuthorizationEndpointConstant.CodeChallengeMethod,
                        AuthorizationEndpointConstant.S256
                    );
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.ResponseType, AuthorizationEndpointConstant.Code);
                    parameters.Add(AuthorizationEndpointConstant.Scope, this.scope1);
                    break;
                case ValReqEx.StateLength:
                    parameters.Add(AuthorizationEndpointConstant.ClientId, this.clientId);
                    parameters.Add(AuthorizationEndpointConstant.RedirectUri, this.redirectUri);
                    parameters.Add(AuthorizationEndpointConstant.State, new string('z', 513));
                    break;
            }
            switch (valCliEx)
            {
                case ValCliEx.InvalidClient:
                    var clientId = "badclient";
                    parameters[AuthorizationEndpointConstant.ClientId] = clientId;
                    this.mockClientStore.Setup(m => m.GetClient(clientId)).ReturnsAsync((OidcClient?)null);
                    break;
                case ValCliEx.GrantMissing:
                case ValCliEx.GrantsNull:
                case ValCliEx.None:
                case ValCliEx.RedirectUriEmpty:
                case ValCliEx.RedirectUriBad:
                case ValCliEx.ScopeInvalid:
                case ValCliEx.ScopesNull:
                    this.mockClientStore.Setup(m => m.GetClient(this.clientId)).ReturnsAsync(oidcClient);
                    break;
                case ValCliEx.NoClient:
                    break;
            }
            // Act
            var (_, redirectError) = await handler.ParseValidateAuthorizationRequest(parameters);
            // Assert
            Assert.IsNotNull(redirectError);
            Assert.AreEqual(expectedExceptionMessage, redirectError.LogMessage);
            Assert.AreEqual(
                EnumHelper.ParseEnumMember<RedirectErrorType>(expectedError),
                redirectError.RedirectErrorType
            );
        }

        [DataTestMethod]
        [DataRow("https://localhost", "something.interested", true, false, false, null)]
        [DataRow("https://localhost", "something.interested", true, true, true, nameof(SecurityTokenExpiredException))]
        [DataRow("https://localhost", "something.interested", false, false, false, null)]
        [DataRow("https://localhost", "something.interested", false, true, false, null)]
        [DataRow(
            "https://localhost",
            "something.inaccessible",
            false,
            false,
            false,
            nameof(SecurityTokenInvalidAudienceException)
        )]
        [DataRow(
            "https://badhost",
            "something.interested",
            false,
            false,
            false,
            nameof(SecurityTokenInvalidIssuerException)
        )]
        [DataRow(
            "https://badhost",
            "something.inaccessible",
            false,
            false,
            false,
            nameof(SecurityTokenInvalidAudienceException)
        )]
        public async Task ValidateJsonWebToken_Success(
            string checkIssuer,
            string checkAudience,
            bool checkExpiry,
            bool isExpired,
            bool logAsInformation,
            string? exceptionType
        )
        {
            // Arrange
            var handler = new ValidationHandler(
                this.mockClientStore.Object,
                this.logger,
                this.providerOptions,
                this.mockSigningKeyHandler.Object,
                this.mockUtcNow.Object
            );
            var issuer = "https://localhost";
            var audience = "something.interested";
            var x509SecurityKey = new X509SecurityKey(TestCertificate.Create(DateTimeOffset.UtcNow));
            var signingKey = new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256).ToSigningKey();
            this.mockSigningKeyHandler.Setup(m => m.Resolve()).ReturnsAsync(new List<SigningKey> { signingKey });
            this.providerOptions.LogFailuresAsInformation = logAsInformation;
            var utcNow = DateTimeOffset.UtcNow;
            var expiry = utcNow.AddMinutes(isExpired ? -5 : 5);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Claims = new Dictionary<string, object>(),
                Expires = expiry.UtcDateTime,
                IssuedAt = utcNow.UtcDateTime,
                Issuer = issuer,
                NotBefore = utcNow.UtcDateTime.AddMinutes(-10),
                SigningCredentials = signingKey.SigningCredentials,
                TokenType = JsonWebTokenConstant.AccessTokenType,
            };
            securityTokenDescriptor.Claims.Add(JsonWebTokenClaim.Audience, audience);
            var jsonWebTokenHandler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
            var jwt = jsonWebTokenHandler.CreateToken(securityTokenDescriptor);
            // Act
            var result = await handler.ValidateJsonWebToken(checkAudience, checkIssuer, jwt, checkExpiry);
            // Assert
            if (string.IsNullOrEmpty(exceptionType))
            {
                Assert.IsNotNull(result);
            }
            else
            {
                Assert.IsNull(result);
                var logLevel = logAsInformation ? LogLevel.Information : LogLevel.Error;
                this.logger.AssertLastItemContains(logLevel, "Json web token validation failed:");
                this.logger.AssertLastItemContains(logLevel, exceptionType);
            }
        }
    }
}
