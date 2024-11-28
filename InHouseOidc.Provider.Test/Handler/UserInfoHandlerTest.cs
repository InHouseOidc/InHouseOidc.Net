// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Handler;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class UserInfoHandlerTest
    {
        private readonly string host = "localhost";
        private readonly string sessionId = "sessionid";
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
        private readonly Address address =
            new()
            {
                Country = "New Zealand",
                Formatted = "1 Somewhere Lane\nSomewhereville\nSomeregion 0000\nNew Zealand",
                Locality = "Somewhereville",
                PostalCode = "0000",
                Region = "Someregion",
                StreetAddress = "1 Somewhere Lane",
            };

        private Mock<IUserStore> mockUserStore = new(MockBehavior.Strict);
        private Mock<IValidationHandler> mockValidationHandler = new(MockBehavior.Strict);

        [TestInitialize]
        public void Initialise()
        {
            this.mockUserStore = new Mock<IUserStore>(MockBehavior.Strict);
            this.mockValidationHandler = new Mock<IValidationHandler>(MockBehavior.Strict);
        }

        [DataTestMethod]
        [DataRow("GET", false)]
        [DataRow("POST", false)]
        [DataRow("POST", true)]
        public async Task UserInfoHandler_HandleRequest(string method, bool useMultipleScopes)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = method;
            context.Request.Scheme = this.urlScheme;
            context.Response.Body = new MemoryStream();
            var accessToken = "access.token";
            if (context.Request.Method == "GET")
            {
                context.Request.Headers.Authorization = $"{JsonWebTokenConstant.Bearer} {accessToken}";
            }
            else
            {
                context.Request.ContentType = ContentTypeConstant.ApplicationForm;
                context.Request.Body = new FormUrlEncodedContent(
                    new Dictionary<string, string> { { JsonWebTokenConstant.AccessToken, accessToken } }
                ).ReadAsStream();
            }
            var issuer = $"{this.urlScheme}://{this.host}";
            var extraClaims = new List<Claim>();
            if (useMultipleScopes)
            {
                extraClaims.AddRange(
                    [
                        new Claim(JsonWebTokenClaim.Scope, "address"),
                        new Claim(JsonWebTokenClaim.Scope, "email"),
                        new Claim(JsonWebTokenClaim.Scope, "phone"),
                        new Claim(JsonWebTokenClaim.Scope, "profile"),
                        new Claim(JsonWebTokenClaim.Scope, "role"),
                    ]
                );
            }
            else
            {
                extraClaims.Add(new Claim(JsonWebTokenClaim.Scope, "address email phone profile nonstd"));
            }
            var (tokenClaimsPrincipal, _) = TestHelper.SetupClaimsPrincipal(
                TimeSpan.Zero,
                ProviderConstant.AuthenticationSchemeCookie,
                this.subject,
                this.sessionId,
                this.utcNow,
                extraClaims
            );
            this.mockValidationHandler.Setup(m => m.ValidateJsonWebToken(null, issuer, accessToken, true))
                .ReturnsAsync(tokenClaimsPrincipal);
            var address = JsonSerializer.Serialize(this.address, JsonHelper.JsonSerializerOptions);
            var email = "joe@bloggs.name";
            var name = "Joe Bloggs";
            var phoneNumber = "+64 (21) 1111111";
            var role1 = "nobody";
            var role2 = "special";
            var userClaims = new List<Claim>
            {
                new(JsonWebTokenClaim.Address, address, "json"),
                new(JsonWebTokenClaim.Email, email),
                new(JsonWebTokenClaim.Name, name),
                new(JsonWebTokenClaim.PhoneNumber, phoneNumber),
                new(JsonWebTokenClaim.Role, role1),
                new(JsonWebTokenClaim.Role, role2),
                new("little_int1", int.MaxValue.ToString(), ClaimValueTypes.Integer),
                new("little_int2", int.MaxValue.ToString(), ClaimValueTypes.Integer32),
                new("bad_int1", "bad", ClaimValueTypes.Integer),
                new("big_int", long.MaxValue.ToString(), ClaimValueTypes.Integer64),
                new("bad_bigint", "bad", ClaimValueTypes.Integer64),
                new("bool1", "true", ClaimValueTypes.Boolean),
                new("bool2", "False", ClaimValueTypes.Boolean),
                new("bad_bool", "negative", ClaimValueTypes.Boolean),
            };
            this.mockUserStore.Setup(m => m.GetUserClaims(issuer, this.subject, It.IsAny<List<string>>()))
                .ReturnsAsync(userClaims);
            var userInfoHandler = new UserInfoHandler(this.mockUserStore.Object, this.mockValidationHandler.Object);
            // Act
            var result = await userInfoHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
            var body = TestHelper.ReadBodyAsString(context.Response);
            Assert.IsNotNull(body);
            var userInfoResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body);
            Assert.IsNotNull(userInfoResponse);
            Assert.AreEqual(13, userInfoResponse.Count);
            // var valueKinds = userInfoResponse.Values.Select(v => v.GetType().Name).ToList();
            var valueKinds = userInfoResponse.Values.Select(v => v.ValueKind).ToList();
            var expectedValueKinds = new JsonValueKind[]
            {
                JsonValueKind.String,
                JsonValueKind.String,
                JsonValueKind.String,
                JsonValueKind.String,
                JsonValueKind.Array,
                JsonValueKind.Number,
                JsonValueKind.Number,
                JsonValueKind.String,
                JsonValueKind.Number,
                JsonValueKind.String,
                JsonValueKind.True,
                JsonValueKind.False,
                JsonValueKind.String,
            };
            CollectionAssert.AreEqual(expectedValueKinds, valueKinds);
        }

        [DataTestMethod]
        [DataRow(
            "DELETE",
            true,
            null,
            null,
            null,
            false,
            ProviderConstant.InvalidHttpMethod,
            "HttpMethod not supported: {method}"
        )]
        [DataRow(
            "POST",
            false,
            null,
            null,
            null,
            false,
            ProviderConstant.InvalidContentType,
            "User info post request used invalid content type"
        )]
        [DataRow("POST", true, null, null, null, false, ProviderConstant.InvalidRequest, "Missing bearer token")]
        [DataRow("POST", true, "", null, null, false, ProviderConstant.InvalidRequest, "Missing bearer token")]
        [DataRow("POST", true, "bad.token", null, null, false, ProviderConstant.InvalidToken, "Bearer token invalid")]
        [DataRow(
            "POST",
            true,
            "good.token",
            null,
            null,
            false,
            ProviderConstant.InvalidToken,
            "Token is missing required claims"
        )]
        [DataRow(
            "POST",
            true,
            "good.token",
            "scope1",
            null,
            false,
            ProviderConstant.InvalidToken,
            "Token is missing required claims"
        )]
        [DataRow(
            "POST",
            true,
            "good.token",
            null,
            "sid",
            false,
            ProviderConstant.InvalidToken,
            "Token is missing required claims"
        )]
        [DataRow(
            "POST",
            true,
            "good.token",
            "scope1",
            "sid",
            true,
            ProviderConstant.InvalidToken,
            "Unable to access user claims"
        )]
        public async Task HandleRequest_UserInfoHandlerExceptions(
            string httpMethod,
            bool goodContent,
            string accessToken,
            string? scope,
            string? subject,
            bool returnEmptyUserClaims,
            string expectedError,
            string expectedExceptionMessage
        )
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = httpMethod;
            context.Request.Scheme = this.urlScheme;
            if (goodContent)
            {
                context.Request.ContentType = ContentTypeConstant.ApplicationForm;
                var formParams = new Dictionary<string, string>();
                if (accessToken == null)
                {
                    // Any form content at all will trigger parameter level validation
                    formParams.Add("placeholder", "value");
                }
                else
                {
                    formParams.Add(JsonWebTokenConstant.AccessToken, accessToken);
                }
                context.Request.Body = new FormUrlEncodedContent(formParams).ReadAsStream();
            }
            context.Response.Body = new MemoryStream();
            var issuer = $"{this.urlScheme}://{this.host}";
            var extraClaims = new List<Claim>();
            if (!string.IsNullOrEmpty(scope))
            {
                extraClaims.Add(new Claim(JsonWebTokenClaim.Scope, scope));
            }
            var (tokenClaimsPrincipal, _) = TestHelper.SetupClaimsPrincipal(
                TimeSpan.Zero,
                ProviderConstant.AuthenticationSchemeCookie,
                subject,
                this.sessionId,
                this.utcNow,
                extraClaims
            );
            if (!string.IsNullOrEmpty(accessToken))
            {
                this.mockValidationHandler.Setup(m => m.ValidateJsonWebToken(null, issuer, accessToken, true))
                    .ReturnsAsync(accessToken == "good.token" ? tokenClaimsPrincipal : null);
            }
            if (returnEmptyUserClaims && subject != null)
            {
                this.mockUserStore.Setup(m => m.GetUserClaims(issuer, subject, It.IsAny<List<string>>()))
                    .ReturnsAsync((List<Claim>?)null);
            }
            var userInfoHandler = new UserInfoHandler(this.mockUserStore.Object, this.mockValidationHandler.Object);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<BadRequestException>(
                async () => await userInfoHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
            Assert.AreEqual(expectedError, exception.Error);
        }
    }
}
