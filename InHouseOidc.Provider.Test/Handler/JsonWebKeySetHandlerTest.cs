﻿// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class JsonWebKeySetHandlerTest
    {
        private readonly string host = "localhost";
        private readonly string urlScheme = "https";

        [DataTestMethod]
        [DataRow("GET", false, null)]
        [DataRow("GET", true, null)]
        [DataRow("POST", false, "HttpMethod not supported: {method}")]
        public async Task HandleRequest(string method, bool setKeys, string? expectedExceptionMessage)
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = method;
            context.Request.Scheme = this.urlScheme;
            context.Response.Body = new MemoryStream();
            var providerOptions = new ProviderOptions();
            var signingKey = (SigningKey?)null;
            var utcNow = new DateTimeOffset(2022, 6, 6, 14, 58, 00, TimeSpan.Zero).ToUniversalTime();
            if (setKeys)
            {
                var x509SecurityKey = new X509SecurityKey(TestCertificate.Create(utcNow));
                signingKey = new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256).ToSigningKey();
                providerOptions.SigningKeys.Add(signingKey);
            }
            var jsonWebKeySetHandler = new JsonWebKeySetHandler(providerOptions);
            // Act/Assert
            if (string.IsNullOrEmpty(expectedExceptionMessage))
            {
                var result = await jsonWebKeySetHandler.HandleRequest(context.Request);
                Assert.IsTrue(result);
                Assert.AreEqual(200, context.Response.StatusCode);
                var body = TestHelper.ReadBodyAsString(context.Response);
                var testJsonWebKeySets = System.Text.Json.JsonSerializer.Deserialize<TestJsonWebKeySets>(body);
                Assert.IsNotNull(testJsonWebKeySets);
                Assert.IsNotNull(testJsonWebKeySets.Keys);
                if (setKeys)
                {
                    Assert.AreEqual(1, testJsonWebKeySets.Keys.Length);
                    Assert.IsNotNull(signingKey);
                    Assert.AreEqual(signingKey.JsonWebKey.KeyId, testJsonWebKeySets.Keys[0].Kid);
                }
                else
                {
                    Assert.AreEqual(0, testJsonWebKeySets.Keys.Length);
                }
            }
            else
            {
                var exception = await Assert.ThrowsExceptionAsync<BadRequestException>(
                    async () => await jsonWebKeySetHandler.HandleRequest(context.Request)
                );
                Assert.IsNotNull(exception);
                Assert.AreEqual(expectedExceptionMessage, exception.LogMessage);
            }
        }

        private class TestJsonWebKeySets
        {
            [JsonPropertyName("keys")]
            public TestJsonWebKey[]? Keys { get; set; }
        }

        private class TestJsonWebKey
        {
            [JsonPropertyName(JsonWebKeySetConstant.Kid)]
            public string? Kid { get; set; }

            [JsonPropertyName(JsonWebKeySetConstant.Use)]
            public string? Use { get; set; }

            [JsonPropertyName(JsonWebKeySetConstant.Kty)]
            public string? Kty { get; set; }

            [JsonPropertyName(JsonWebKeySetConstant.Alg)]
            public string? Alg { get; set; }

            [JsonPropertyName(JsonWebKeySetConstant.E)]
            public string? E { get; set; }

            [JsonPropertyName(JsonWebKeySetConstant.N)]
            public string? N { get; set; }

            [JsonPropertyName(JsonWebKeySetConstant.X5t)]
            public string? X5t { get; set; }

            [JsonPropertyName(JsonWebKeySetConstant.X5c)]
            public string[]? X5c { get; set; }
        }
    }
}
