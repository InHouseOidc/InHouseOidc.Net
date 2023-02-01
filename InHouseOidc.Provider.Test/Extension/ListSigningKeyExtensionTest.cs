// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace InHouseOidc.Provider.Test.Extension
{
    [TestClass]
    public class ListSigningKeyExtensionTest
    {
        [TestMethod]
        public void ResolvePreset_Success()
        {
            // Arrange
            var x509Certificate2 = TestCertificate.Create(DateTimeOffset.UtcNow);
            var listSigningKey = new List<SigningKey>();
            listSigningKey.StoreSigningKeys(new List<X509Certificate2> { x509Certificate2 });
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Act
            var result = listSigningKey.Resolve(serviceProvider);
            // Assert
            Assert.AreSame(listSigningKey, result);
            Assert.AreEqual(1, result.Count);
        }

        [TestMethod]
        public void ResolveFromStore_Success()
        {
            // Arrange
            var x509Certificate2 = TestCertificate.Create(DateTimeOffset.UtcNow);
            var serviceCollection = new TestServiceCollection();
            var mockCertificateStore = new Mock<ICertificateStore>(MockBehavior.Strict);
            mockCertificateStore
                .Setup(m => m.GetSigningCertificates())
                .ReturnsAsync(new List<X509Certificate2> { x509Certificate2 });
            serviceCollection.AddSingleton(mockCertificateStore.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var listSigningKey = new List<SigningKey>();
            // Act
            var result = listSigningKey.Resolve(serviceProvider);
            // Assert
            Assert.AreSame(listSigningKey, result);
            Assert.AreEqual(1, result.Count);
            mockCertificateStore.VerifyAll();
        }

        [TestMethod]
        public void ResolveFromStore_NoCertificates()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var mockCertificateStore = new Mock<ICertificateStore>(MockBehavior.Strict);
            mockCertificateStore.Setup(m => m.GetSigningCertificates()).ReturnsAsync(new List<X509Certificate2>());
            serviceCollection.AddSingleton(mockCertificateStore.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var listSigningKey = new List<SigningKey>();
            // Act
            var exception = Assert.ThrowsException<InternalErrorException>(
                () => listSigningKey.Resolve(serviceProvider)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(
                "No signing keys available.  Set via ProviderBuilder.SetSigningCertificates"
                    + " or implement ICertificateStore.GetSigningCertificates",
                exception.LogMessage
            );
            mockCertificateStore.VerifyAll();
        }
    }
}
