// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Exception;
using InHouseOidc.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Provider.Test.Extension
{
    [TestClass]
    public class ListSigningKeyExtensionTest
    {
        [TestMethod]
        public void ListSigningKeyExtension_StoreSigningKeys_NoPrivateKey()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            // Act
            var exception = Assert.Throws<InternalErrorException>(
                () => providerBuilder.SetSigningCertificates([TestCertificate.CreatePublicOnly(DateTimeOffset.UtcNow)])
            );
            // Assert
            Assert.Contains("must include a private key", exception.LogMessage);
        }

        [TestMethod]
        public void ListSigningKeyExtension_StoreSigningKeys_NotRS256()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            // Act
            var exception = Assert.Throws<InternalErrorException>(
                () => providerBuilder.SetSigningCertificates([TestCertificate.CreateNonRS256(DateTimeOffset.UtcNow)])
            );
            // Assert
            Assert.Contains("must support RS256 algorithm", exception.LogMessage);
        }
    }
}
