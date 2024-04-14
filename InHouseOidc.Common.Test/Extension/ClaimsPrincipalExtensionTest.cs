// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Common.Test.Extension
{
    [TestClass]
    public class ClaimsPrincipalExtensionTest
    {
        [TestMethod]
        public void GetAuthenticationTimeClaim_Success()
        {
            // Arrange
            var time = new DateTimeOffset(2022, 5, 4, 8, 18, 0, 0, TimeSpan.FromHours(12));
            var claims = new List<Claim>
            {
                new(JsonWebTokenClaim.AuthenticationTime, time.ToUnixTimeSeconds().ToString()),
            };
            var claimsIdentity = new ClaimsIdentity(claims, "testscheme");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            // Act
            var authenticationTime = claimsPrincipal.GetAuthenticationTimeClaim();
            // Assert
            Assert.AreEqual(time, authenticationTime);
        }

        [TestMethod]
        public void GetAuthenticationTimeClaim_NotFound()
        {
            // Arrange
            var claims = new List<Claim>();
            var claimsIdentity = new ClaimsIdentity(claims, "testscheme");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            // Act
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => claimsPrincipal.GetAuthenticationTimeClaim()
            );
            // Assert
            StringAssert.Contains(exception.Message, "auth_time claim not found");
        }

        [TestMethod]
        public void GetSessionIdClaim_Success()
        {
            // Arrange
            var sessionId = "id";
            var claims = new List<Claim> { new(JsonWebTokenClaim.SessionId, sessionId) };
            var claimsIdentity = new ClaimsIdentity(claims, "testscheme");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            // Act
            var result = claimsPrincipal.GetSessionIdClaim();
            // Assert
            Assert.AreEqual(sessionId, result);
        }

        [TestMethod]
        public void GetSessionIdClaim_NotFound()
        {
            // Arrange
            var claims = new List<Claim>();
            var claimsIdentity = new ClaimsIdentity(claims, "testscheme");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            // Act
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => claimsPrincipal.GetSessionIdClaim()
            );
            // Assert
            StringAssert.Contains(exception.Message, "sid claim not found");
        }

        [TestMethod]
        public void GetSubjectClaim_Success()
        {
            // Arrange 1 (JsonWebTokenClaim.Subject)
            var subject = "subject";
            var claims = new List<Claim> { new(JsonWebTokenClaim.Subject, subject) };
            var claimsIdentity = new ClaimsIdentity(claims, "testscheme");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            // Act 1
            var result1 = claimsPrincipal.GetSubjectClaim();
            // Assert 1
            Assert.AreEqual(subject, result1);
            // Arrange 2 (ClaimTypes.NameIdentifier)
            claims = [new(ClaimTypes.NameIdentifier, subject)];
            claimsIdentity = new ClaimsIdentity(claims, "testscheme");
            claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            // Act 2
            var result2 = claimsPrincipal.GetSubjectClaim();
            // Assert 1
            Assert.AreEqual(subject, result2);
        }

        [TestMethod]
        public void GetSubjectClaim_NotFound()
        {
            // Arrange
            var claims = new List<Claim>();
            var claimsIdentity = new ClaimsIdentity(claims, "testscheme");
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            // Act
            var exception = Assert.ThrowsException<InvalidOperationException>(() => claimsPrincipal.GetSubjectClaim());
            // Assert
            StringAssert.Contains(exception.Message, "sub/nameidentifier claim not found");
        }
    }
}
