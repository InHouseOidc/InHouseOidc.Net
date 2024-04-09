// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider.Handler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class ProviderTokenHandlerTest
    {
        [TestMethod]
        public async Task GetProviderAccessToken()
        {
            // Arrange
            var clientId = "client";
            var issuer = "https://localhost";
            var scopes = new List<string> { "scope1", "scope2" };
            var token = "abc123";
            var utcNow = new DateTimeOffset(2022, 5, 26, 7, 42, 00, TimeSpan.Zero).ToUniversalTime();
            var expires = TimeSpan.FromMinutes(15);
            var mockJsonWebTokenHandler = new Mock<IJsonWebTokenHandler>(MockBehavior.Strict);
            mockJsonWebTokenHandler
                .Setup(m => m.GetAccessToken(clientId, utcNow + expires, issuer, scopes, null))
                .ReturnsAsync(token);
            var mockUtcNow = new Mock<IUtcNow>(MockBehavior.Strict);
            mockUtcNow.Setup(m => m.UtcNow).Returns(utcNow);
            var providerTokenHandler = new ProviderTokenHandler(mockJsonWebTokenHandler.Object, mockUtcNow.Object);
            // Act
            var result = await providerTokenHandler.GetProviderAccessToken(clientId, expires, issuer, scopes);
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(token, result);
        }
    }
}
