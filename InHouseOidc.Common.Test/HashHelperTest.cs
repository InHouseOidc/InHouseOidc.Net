// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Common.Test
{
    [TestClass]
    public class HashHelperTest
    {
        [TestMethod]
        public void HashCodeVerifierS256()
        {
            // Arrange
            var codeVerifier = "abc123";
            // Act
            var result = HashHelper.HashCodeVerifierS256(codeVerifier);
            // Assert
            Assert.AreEqual("bKE9UspwyIPg8LsQHkJaiehiTeUdstI5JZOvaoQRgJA", result);
        }

        [TestMethod]
        public void GenerateCode()
        {
            // Act
            var result = HashHelper.GenerateCode();
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length >= 86, $"Unexpectedly short generated code length of {result.Length}");
        }

        [TestMethod]
        public void GenerateSessionId()
        {
            // Act
            var result = HashHelper.GenerateSessionId();
            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length >= 22, $"Unexpectedly short generated session id length of {result.Length}");
        }

        [TestMethod]
        public void GenerateSessionState()
        {
            // Act 1
            var clientId = "clid";
            var redirectUriString = "https://localhost/return-uri";
            var sessionId = HashHelper.GenerateSessionId();
            var result = HashHelper.GenerateSessionState(null, clientId, redirectUriString, sessionId);
            // Assert 1
            Assert.IsNotNull(result);
            StringAssert.Contains(result, ".");
            Assert.IsTrue(result.Length >= 66, $"Unexpectedly short generated session state length of {result.Length}");
            // Assert 2
            var sessionStateParts = result.Split(".");
            Assert.AreEqual(2, sessionStateParts.Length);
            Assert.AreEqual(
                HashHelper.GenerateSessionState(sessionStateParts[1], clientId, redirectUriString, sessionId),
                result
            );
        }
    }
}
