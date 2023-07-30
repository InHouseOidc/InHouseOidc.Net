// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class SigningKeyHandlerTest
    {
        private Mock<IAsyncLock<SigningKeyHandler>> mockAsyncLock = new(MockBehavior.Strict);
        private Mock<ICertificateStore> mockCertificateStore = new(MockBehavior.Strict);

        private Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private ProviderOptions providerOptions = new();
        private DateTimeOffset utcNow = new DateTimeOffset(2022, 5, 12, 17, 33, 00, TimeSpan.Zero).ToUniversalTime();

        [TestInitialize]
        public void Initialise()
        {
            this.mockAsyncLock = new Mock<IAsyncLock<SigningKeyHandler>>();
            this.mockUtcNow = new Mock<IUtcNow>(MockBehavior.Strict);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(() => this.utcNow);
            this.providerOptions = new();
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Resolve_Success(bool fromStore)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var x509Certificate2 = TestCertificate.Create(DateTimeOffset.UtcNow);
            var storeCertificates = new List<X509Certificate2>();
            if (fromStore)
            {
                storeCertificates.Add(x509Certificate2);
                this.mockCertificateStore.Setup(m => m.GetSigningCertificates()).ReturnsAsync(storeCertificates);
                serviceCollection.AddSingleton(this.mockCertificateStore.Object);
            }
            else
            {
                var x509SecurityKey = new X509SecurityKey(x509Certificate2);
                var signingKey = new SigningCredentials(x509SecurityKey, SecurityAlgorithms.RsaSha256).ToSigningKey();
                this.providerOptions.SigningKeys.Add(signingKey);
            }
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var signingKeyHandler = new SigningKeyHandler(
                this.mockAsyncLock.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act 1 (resolve)
            var results1 = await signingKeyHandler.Resolve();
            // Assert 1
            Assert.IsNotNull(results1);
            Assert.AreEqual(1, results1.Count);
            Assert.AreEqual(x509Certificate2.Thumbprint, results1[0].SigningCredentials.Kid);
            if (fromStore)
            {
                this.mockCertificateStore.Verify(m => m.GetSigningCertificates(), Times.Once());
            }
            // Act 2 (expired, re-resolve)
            this.utcNow = this.utcNow.AddHours(24);
            var results2 = await signingKeyHandler.Resolve();
            // Assert 2
            Assert.IsNotNull(results2);
            Assert.AreEqual(1, results2.Count);
            Assert.AreEqual(x509Certificate2.Thumbprint, results2[0].SigningCredentials.Kid);
            if (fromStore)
            {
                this.mockCertificateStore.Verify(m => m.GetSigningCertificates(), Times.Exactly(2));
            }
            // Act 3 (cached)
            this.providerOptions.SigningKeys.Clear();
            storeCertificates.Clear();
            var results3 = await signingKeyHandler.Resolve();
            // Assert 3
            Assert.IsNotNull(results3);
            Assert.AreEqual(1, results3.Count);
            Assert.AreEqual(x509Certificate2.Thumbprint, results3[0].SigningCredentials.Kid);
            if (fromStore)
            {
                this.mockCertificateStore.Verify(m => m.GetSigningCertificates(), Times.Exactly(2));
            }
        }

        [TestMethod]
        public async Task Resolve_NoKeysException()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var signingKeyHandler = new SigningKeyHandler(
                this.mockAsyncLock.Object,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InternalErrorException>(
                () => signingKeyHandler.Resolve()
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains(exception.LogMessage, "No signing keys available");
        }

        [TestMethod]
        public async Task Resolve_LockingThreadRace()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton(this.mockCertificateStore.Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var x509Certificate2 = TestCertificate.Create(DateTimeOffset.UtcNow);
            var storeCertificates = new List<X509Certificate2> { x509Certificate2 };
            this.mockCertificateStore.Setup(m => m.GetSigningCertificates()).ReturnsAsync(storeCertificates);
            var asyncLock = new AsyncLock<SigningKeyHandler>();
            var signingKeyHandler = new SigningKeyHandler(
                asyncLock,
                this.providerOptions,
                serviceProvider,
                this.mockUtcNow.Object
            );
            // Act 1 (cache)
            var results0 = await signingKeyHandler.Resolve();
            // Assert 1
            Assert.IsNotNull(results0);
            Assert.AreEqual(1, results0.Count);
            Assert.AreEqual(x509Certificate2.Thumbprint, results0[0].SigningCredentials.Kid);
            this.mockCertificateStore.Verify(m => m.GetSigningCertificates(), Times.Once());
            // Act 2 (launch 2 resolves that wait on the async lock)
            var releaser = asyncLock.Lock();
            List<SigningKey>? results1 = null;
            var waiter1 = Task.Run(async () =>
            {
                results1 = await signingKeyHandler.Resolve();
            });
            List<SigningKey>? results2 = null;
            var waiter2 = Task.Run(async () =>
            {
                results2 = await signingKeyHandler.Resolve();
            });
            // Assert 2
            Assert.IsNull(results1);
            Assert.IsFalse(waiter1.IsCompleted);
            Assert.IsNull(results2);
            Assert.IsFalse(waiter2.IsCompleted);
            // Act 3 (release the lock and let the race commence)
            this.utcNow = this.utcNow.AddHours(24);
            releaser.Dispose();
            // Assert 3 (both threads resolve, only 1 thread does the reload work)
            while (!waiter1.IsCompleted || !waiter2.IsCompleted)
            {
                await Task.Delay(50);
            }
            Assert.IsNotNull(results1);
            Assert.IsTrue(waiter1.IsCompleted);
            Assert.IsNotNull(results2);
            Assert.IsTrue(waiter2.IsCompleted);
            this.mockCertificateStore.Verify(m => m.GetSigningCertificates(), Times.Exactly(2));
        }
    }
}
