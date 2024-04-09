// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Extension;
using InHouseOidc.Test.Common;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Provider.Test.Extension
{
    [TestClass]
    public class SigningCredentialsExtensionTest
    {
        [TestMethod]
        public void ToSigningKey_Success()
        {
            // Arrange
            var x509SecurityKey = new X509SecurityKey(TestCertificate.Create(DateTimeOffset.UtcNow));
            var signingCredentials = new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256);
            // Act
            var result = signingCredentials.ToSigningKey();
            // Assert
            CollectionAssert.AreEqual(x509SecurityKey.Certificate.GetCertHash(), result.CertificateHash);
        }

        [TestMethod]
        public void ToSigningKey_NotX509SecurityKey()
        {
            // Arrange
            var provider = new RSACryptoServiceProvider(2048);
            var securityKey = new RsaSecurityKey(provider);
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
            // Act
            var exception = Assert.ThrowsException<ArgumentException>(() => signingCredentials.ToSigningKey());
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(
                "Signing credentials must use X509SecurityKey (Parameter 'signingCredentials')",
                exception.Message
            );
        }

        [TestMethod]
        public void ToSigningKey_NotRsaSecurityKey()
        {
            // Arrange
            var securityKey = new X509SecurityKey(TestCertificate.CreateNonRS256(DateTimeOffset.UtcNow));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
            // Act
            var exception = Assert.ThrowsException<ArgumentException>(() => signingCredentials.ToSigningKey());
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("Signing credentials must use RSA public key", exception.Message);
        }
    }
}
