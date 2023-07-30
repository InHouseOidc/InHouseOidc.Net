// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class JsonWebTokenHandlerTest
    {
        private readonly Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private readonly List<string> audiences = new() { "https://api1.app", "https://api2.app" };
        private readonly string clientId = "clientid";
        private readonly string email = "joe@bloggs.name";
        private readonly string issuer = "https://localhost";
        private readonly string redirectUri = "https://client.app/auth/callback";
        private readonly List<string> scopes = new() { "scope1", "scope2" };
        private readonly string sessionId = "session";
        private readonly string subject = "subjectid";
        private readonly DateTimeOffset expiry = new DateTimeOffset(
            2022,
            5,
            17,
            17,
            28,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();
        private readonly DateTimeOffset utcNow = new DateTimeOffset(
            2022,
            5,
            17,
            17,
            13,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();
        private readonly Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler handler = new();
        private ProviderOptions providerOptions = new();
        private Mock<IResourceStore> mockResourceStore = new(MockBehavior.Strict);
        private Mock<ISigningKeyHandler> mockSigningKeyHandler = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.providerOptions = new();
            this.mockResourceStore = new Mock<IResourceStore>(MockBehavior.Strict);
            this.mockResourceStore.Setup(m => m.GetAudiences(this.scopes)).ReturnsAsync(this.audiences);
            this.mockSigningKeyHandler = new Mock<ISigningKeyHandler>(MockBehavior.Strict);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
        }

        [DataTestMethod]
        [DataRow("subjectid")]
        [DataRow(null)]
        public async Task GetAccessToken(string? subjectId)
        {
            // Arrange
            var x509SecurityKey = new X509SecurityKey(TestCertificate.Create(this.utcNow));
            var signingKey = new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256).ToSigningKey();
            this.mockSigningKeyHandler.Setup(m => m.Resolve()).ReturnsAsync(new List<SigningKey> { signingKey });
            var jsonWebTokenHandler = new JsonWebTokenHandler(
                this.providerOptions,
                this.mockResourceStore.Object,
                this.mockSigningKeyHandler.Object,
                this.mockUtcNow.Object
            );
            // Act
            var result = await jsonWebTokenHandler.GetAccessToken(
                this.clientId,
                this.expiry,
                this.issuer,
                this.scopes,
                subjectId
            );
            // Assert
            Assert.IsNotNull(result);
            var securityToken = this.handler.ReadToken(result);
            var jsonWebToken = securityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
            Assert.IsNotNull(jsonWebToken);
            Assert.AreEqual(signingKey.JsonWebKey.Kid, jsonWebToken.Kid);
            Assert.AreEqual(this.issuer, securityToken.Issuer);
            Assert.AreEqual(this.expiry.DateTime, securityToken.ValidTo);
            AssertHasClaim(jsonWebToken, JsonWebTokenClaim.ClientId, this.clientId);
            if (string.IsNullOrEmpty(subjectId))
            {
                Assert.IsFalse(
                    jsonWebToken.Claims.Any(c => c.Type == JsonWebTokenClaim.Subject),
                    "Existence of subject claim was unexpected"
                );
            }
            else
            {
                AssertHasClaim(jsonWebToken, JsonWebTokenClaim.Subject, subjectId);
            }
            AssertHasClaims(jsonWebToken, JsonWebTokenClaim.Audience, this.audiences);
            AssertHasClaims(jsonWebToken, JsonWebTokenClaim.Scope, this.scopes);
        }

        [DataTestMethod]
        [DataRow("onlyonce", "pwd", "superuser")]
        [DataRow(null, null, null)]
        public async Task GetIdToken_Success(string? nonce, string? amr, string? role)
        {
            // Arrange
            var x509SecurityKey = new X509SecurityKey(TestCertificate.Create(this.utcNow));
            var signingKey = new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256).ToSigningKey();
            this.mockSigningKeyHandler.Setup(m => m.Resolve()).ReturnsAsync(new List<SigningKey> { signingKey });
            var jsonWebTokenHandler = new JsonWebTokenHandler(
                this.providerOptions,
                this.mockResourceStore.Object,
                this.mockSigningKeyHandler.Object,
                this.mockUtcNow.Object
            );
            var address =
                "{\"formatted\":\"1 Somewhere Lane\\\\nSomewhereville\\\\nSomeregion 0000\\\\nNew Zealand\",\"street_address\":\"1 Somewhere Lane\",\"locality\":\"Somewhereville\",\"region\":\"Someregion\",\"postal_code\":\"0000\",\"country\":\"New Zealand\"}";
            var name = "Joe Bloggs";
            var phoneNumber = "+64 (21) 1111111";
            var scopes = new List<string>
            {
                JsonWebTokenConstant.Address,
                JsonWebTokenConstant.Email,
                JsonWebTokenConstant.Phone,
                JsonWebTokenConstant.Profile,
            };
            if (!string.IsNullOrEmpty(role))
            {
                scopes.Add(JsonWebTokenConstant.Role);
            }
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                string.Join(' ', scopes)
            );
            if (!string.IsNullOrEmpty(nonce))
            {
                authorizationRequest.Nonce = nonce;
            }
            authorizationRequest.SessionExpiryUtc = this.expiry;
            authorizationRequest.AuthorizationRequestClaims.AddRange(
                new AuthorizationRequestClaim[]
                {
                    new AuthorizationRequestClaim(
                        JsonWebTokenClaim.AuthenticationTime,
                        this.utcNow.ToUnixTimeSeconds().ToString()
                    ),
                    new AuthorizationRequestClaim(
                        JsonWebTokenClaim.IdentityProvider,
                        this.providerOptions.IdentityProvider
                    ),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.SessionId, this.sessionId),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.Address, address),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.Email, this.email),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.PhoneNumber, phoneNumber),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.Name, name),
                }
            );
            if (!string.IsNullOrEmpty(amr))
            {
                authorizationRequest.AuthorizationRequestClaims.Add(
                    new AuthorizationRequestClaim(JsonWebTokenClaim.AuthenticationMethodReference, amr)
                );
            }
            if (!string.IsNullOrEmpty(role))
            {
                authorizationRequest.AuthorizationRequestClaims.Add(
                    new AuthorizationRequestClaim(JsonWebTokenClaim.Role, role)
                );
            }
            // Act
            var result = await jsonWebTokenHandler.GetIdToken(
                authorizationRequest,
                this.clientId,
                this.issuer,
                scopes,
                this.subject
            );
            // Assert
            Assert.IsNotNull(result);
            var securityToken = this.handler.ReadToken(result);
            var jsonWebToken = securityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
            Assert.IsNotNull(jsonWebToken);
            Assert.AreEqual(signingKey.JsonWebKey.Kid, jsonWebToken.Kid);
            Assert.AreEqual(this.issuer, securityToken.Issuer);
            Assert.AreEqual(this.expiry, securityToken.ValidTo);
            AssertHasClaim(jsonWebToken, JsonWebTokenClaim.Subject, this.subject);
            AssertHasClaim(jsonWebToken, JsonWebTokenClaim.Address, address);
            AssertHasClaim(jsonWebToken, JsonWebTokenClaim.Email, this.email);
            AssertHasClaim(jsonWebToken, JsonWebTokenClaim.PhoneNumber, phoneNumber);
            AssertHasClaim(jsonWebToken, JsonWebTokenClaim.Name, name);
            AssertHasClaims(jsonWebToken, JsonWebTokenClaim.Audience, new[] { this.clientId });
            if (!string.IsNullOrEmpty(nonce))
            {
                AssertHasClaim(jsonWebToken, JsonWebTokenClaim.Nonce, nonce);
            }
            if (!string.IsNullOrEmpty(amr))
            {
                AssertHasClaims(
                    jsonWebToken,
                    JsonWebTokenClaim.AuthenticationMethodReference,
                    new List<string> { amr }
                );
            }
            if (!string.IsNullOrEmpty(role))
            {
                AssertHasClaims(jsonWebToken, JsonWebTokenClaim.Role, new List<string> { role });
            }
        }

        [DataTestMethod]
        [DataRow(false, false, "AuthorizationRequest has no value for SessionExpiryUtc")]
        [DataRow(true, true, "Unable to resolve signing credentials for JWT")]
        public async Task GetIdToken_Exceptions(
            bool setExpiry,
            bool setExpiredCertificate,
            string expectedExceptionMessage
        )
        {
            // Arrange
            var x509SecurityKey = new X509SecurityKey(
                setExpiredCertificate ? TestCertificate.CreateExpired(this.utcNow) : TestCertificate.Create(this.utcNow)
            );
            var signingKey = new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256).ToSigningKey();
            this.mockSigningKeyHandler.Setup(m => m.Resolve()).ReturnsAsync(new List<SigningKey> { signingKey });
            var scopes = new List<string> { JsonWebTokenConstant.Email };
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                string.Join(' ', scopes)
            );
            authorizationRequest.AuthorizationRequestClaims.AddRange(
                new AuthorizationRequestClaim[]
                {
                    new AuthorizationRequestClaim(
                        JsonWebTokenClaim.AuthenticationTime,
                        this.utcNow.ToUnixTimeSeconds().ToString()
                    ),
                    new AuthorizationRequestClaim(
                        JsonWebTokenClaim.IdentityProvider,
                        this.providerOptions.IdentityProvider
                    ),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.SessionId, this.sessionId),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.Email, this.email),
                }
            );
            if (setExpiry)
            {
                authorizationRequest.SessionExpiryUtc = this.expiry;
            }
            var jsonWebTokenHandler = new JsonWebTokenHandler(
                this.providerOptions,
                this.mockResourceStore.Object,
                this.mockSigningKeyHandler.Object,
                this.mockUtcNow.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InternalErrorException>(
                () =>
                    jsonWebTokenHandler.GetIdToken(
                        authorizationRequest,
                        this.clientId,
                        this.issuer,
                        scopes,
                        this.subject
                    )
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains(exception.LogMessage, expectedExceptionMessage);
        }

        [TestMethod]
        public async Task SelectOptimalSigningKey()
        {
            // Arrange
            var signingKey = new SigningCredentials(
                new X509SecurityKey(TestCertificate.Create(this.utcNow)),
                SecurityAlgorithms.RsaSha256
            ).ToSigningKey();
            var signingKeys = new List<SigningKey>
            {
                new SigningCredentials(
                    new X509SecurityKey(TestCertificate.CreateExpired(this.utcNow)),
                    SecurityAlgorithms.RsaSha256
                ).ToSigningKey(),
                new SigningCredentials(
                    new X509SecurityKey(TestCertificate.CreateNearExpired(this.utcNow)),
                    SecurityAlgorithms.RsaSha256
                ).ToSigningKey(),
                new SigningCredentials(
                    new X509SecurityKey(TestCertificate.CreateNotReady(this.utcNow)),
                    SecurityAlgorithms.RsaSha256
                ).ToSigningKey(),
                signingKey,
            };
            this.mockSigningKeyHandler.Setup(m => m.Resolve()).ReturnsAsync(signingKeys);
            var scopes = new List<string> { JsonWebTokenConstant.Email };
            var authorizationRequest = new AuthorizationRequest(
                this.clientId,
                this.redirectUri,
                ResponseType.Code,
                string.Join(' ', scopes)
            );
            authorizationRequest.AuthorizationRequestClaims.AddRange(
                new AuthorizationRequestClaim[]
                {
                    new AuthorizationRequestClaim(
                        JsonWebTokenClaim.AuthenticationTime,
                        this.utcNow.ToUnixTimeSeconds().ToString()
                    ),
                    new AuthorizationRequestClaim(
                        JsonWebTokenClaim.IdentityProvider,
                        this.providerOptions.IdentityProvider
                    ),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.SessionId, this.sessionId),
                    new AuthorizationRequestClaim(JsonWebTokenClaim.Email, this.email),
                }
            );
            authorizationRequest.SessionExpiryUtc = this.expiry;
            var jsonWebTokenHandler = new JsonWebTokenHandler(
                this.providerOptions,
                this.mockResourceStore.Object,
                this.mockSigningKeyHandler.Object,
                this.mockUtcNow.Object
            );
            // Act
            var result = await jsonWebTokenHandler.GetIdToken(
                authorizationRequest,
                this.clientId,
                this.issuer,
                scopes,
                this.subject
            );
            // Assert
            Assert.IsNotNull(result);
            var securityToken = this.handler.ReadToken(result);
            var jsonWebToken = securityToken as Microsoft.IdentityModel.JsonWebTokens.JsonWebToken;
            Assert.IsNotNull(jsonWebToken);
            Assert.AreEqual(signingKey.JsonWebKey.Kid, jsonWebToken.Kid);
        }

        private static void AssertHasClaim(
            Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken,
            string claimType,
            string claimValue
        )
        {
            var claims = jsonWebToken.Claims.Where(c => c.Type == claimType).ToList();
            if (!claims.Any())
            {
                Assert.Fail($"Unable to find claimType: {claimType}");
            }
            if (claims.Count > 1)
            {
                Assert.Fail($"Found multiple values for claimType: {claimType}");
            }
            Assert.AreEqual(claimValue, claims.First().Value, $"Incorrect value for claim: {claimType}");
        }

        private static void AssertHasClaims(
            Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonWebToken,
            string claimType,
            IEnumerable<string> claimValues
        )
        {
            var claims = jsonWebToken.Claims.Where(c => c.Type == claimType).Select(c => c.Value).ToList();
            if (!claims.Any())
            {
                Assert.Fail($"Unable to find claimType: {claimType}");
            }
            CollectionAssert.AreEqual(claims, claimValues.ToList(), $"Incorrect values for claim: {claimType}");
        }
    }
}
