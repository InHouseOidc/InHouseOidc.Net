// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Type;
using InHouseOidc.CredentialsClient.Resolver;
using InHouseOidc.CredentialsClient.Type;
using InHouseOidc.Discovery;
using InHouseOidc.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.CredentialsClient.Test.Resolver
{
    [TestClass]
    public class ClientCredentialsResolverTest
    {
        private readonly TestLogger<ClientCredentialsResolver> logger = new();
        private readonly DateTimeOffset utcNow = new DateTimeOffset(
            2022,
            4,
            23,
            18,
            22,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();
        private readonly string clientName = "testclient";
        private readonly TokenResponse tokenResponse = new() { AccessToken = "accesstoken", ExpiresIn = 600 };

        private Mock<IAsyncLock<ClientCredentialsResolver>> mockAsyncLock = new(MockBehavior.Strict);
        private ClientOptions clientOptions = new();
        private Mock<IDiscoveryResolver> mockDiscoveryResolver = new(MockBehavior.Strict);
        private Mock<IHttpClientFactory> mockHttpClientFactory = new(MockBehavior.Strict);
        private TestMessageHandler testMessageHandler = new();
        private Mock<IUtcNow> mockUtcNow = new(MockBehavior.Strict);
        private Mock<IServiceProvider> mockServiceProvider = new(MockBehavior.Strict);
        private CredentialsClientOptions credentialsClientOptions = new();
        private Discovery.Discovery? discovery;

        [TestInitialize]
        public void Initialise()
        {
            this.mockAsyncLock = new Mock<IAsyncLock<ClientCredentialsResolver>>();
            this.clientOptions = new();
            this.mockDiscoveryResolver = new(MockBehavior.Strict);
            this.mockHttpClientFactory = new(MockBehavior.Strict);
            this.testMessageHandler = new TestMessageHandler();
            this.mockHttpClientFactory.Setup(m => m.CreateClient(this.clientOptions.InternalHttpClientName))
                .Returns(new HttpClient(this.testMessageHandler));
            this.mockServiceProvider = new(MockBehavior.Strict);
            this.mockUtcNow = new Mock<IUtcNow>(MockBehavior.Strict);
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow);
            this.credentialsClientOptions = new CredentialsClientOptions
            {
                ClientId = this.clientName,
                ClientSecret = "TopSecret",
                OidcProviderAddress = "https://localhost",
                Scope = "scope1",
            };
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            this.discovery = new Discovery.Discovery(
                null,
                null,
                null,
                DateTimeOffset.MaxValue,
                ["code"],
                this.credentialsClientOptions.OidcProviderAddress,
                "/token",
                [DiscoveryConstant.ClientSecretPost]
            );
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_Success()
        {
            // Arrange
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, this.credentialsClientOptions);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(this.tokenResponse),
                StatusCode = HttpStatusCode.OK,
            };
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(this.discovery);
            // Act 1 (uncached)
            var result1 = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert 1
            Assert.IsNotNull(result1);
            Assert.AreEqual("accesstoken", result1);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            this.mockDiscoveryResolver.VerifyAll();
            this.mockHttpClientFactory.VerifyAll();
            // Act 2 (cached)
            var result2 = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert 2
            this.mockUtcNow.VerifyAll();
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            Assert.IsNotNull(result2);
            Assert.AreEqual("accesstoken", result2);
            // Act 3 (cleared cache)
            await clientCredentialsResolver.ClearClientToken(this.clientName);
            var result3 = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert 3
            Assert.AreEqual(2, this.testMessageHandler.SendCount);
            Assert.IsNotNull(result3);
            Assert.AreEqual("accesstoken", result3);
            // Act 4 (expired)
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow.AddHours(1));
            var result4 = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            Assert.AreEqual(3, this.testMessageHandler.SendCount);
            Assert.IsNotNull(result4);
            Assert.AreEqual("accesstoken", result4);
        }

        [DataTestMethod]
        [DataRow(false, "Client credentials options not available via AddClient or ICredentialsStore")]
        [DataRow(true, "Client credentials options not available from ICredentialsStore")]
        public async Task ClientCredentialsResolver_BadClientName(bool setupCredentialsStore, string expectedMessage)
        {
            // Arrange
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            var clientName = "badclientname";
            if (setupCredentialsStore)
            {
                var mockCredentialsStore = new Mock<ICredentialsStore>(MockBehavior.Strict);
                mockCredentialsStore
                    .Setup(m => m.GetCredentialsClientOptions(clientName))
                    .Returns(Task.FromResult<CredentialsClientOptions?>(null));
                this.mockServiceProvider.Setup(m => m.GetService(typeof(ICredentialsStore)))
                    .Returns(mockCredentialsStore.Object);
            }
            else
            {
                this.mockServiceProvider.Setup(m => m.GetService(typeof(ICredentialsStore))).Returns((object?)null);
            }
            // Act
            var result = await clientCredentialsResolver.GetClientToken(clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.logger.AssertLastItemContains(LogLevel.Error, expectedMessage);
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_ClientFromStore()
        {
            // Arrange
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, null);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            var mockCredentialsStore = new Mock<ICredentialsStore>(MockBehavior.Strict);
            mockCredentialsStore
                .Setup(m => m.GetCredentialsClientOptions(this.clientName))
                .ReturnsAsync(this.credentialsClientOptions);
            this.mockServiceProvider.Setup(m => m.GetService(typeof(ICredentialsStore)))
                .Returns(mockCredentialsStore.Object);
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(this.tokenResponse),
                StatusCode = HttpStatusCode.OK,
            };
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(this.discovery);
            // Act 1
            var result1 = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert 1
            Assert.IsNotNull(result1);
            Assert.AreEqual("accesstoken", result1);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            mockCredentialsStore.VerifyAll();
            // Act 2 (expired)
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow.AddSeconds(1200));
            var result2 = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            Assert.IsNotNull(result2);
            Assert.AreEqual("accesstoken", result2);
            Assert.AreEqual(2, this.testMessageHandler.SendCount);
            // Act 3 (cached)
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow.AddSeconds(1230));
            var result3 = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            Assert.IsNotNull(result3);
            Assert.AreEqual("accesstoken", result3);
            Assert.AreEqual(2, this.testMessageHandler.SendCount);
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_BadClientOptions()
        {
            // Arrange
            var credentialsClientOptions = new CredentialsClientOptions();
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, credentialsClientOptions);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                async () => await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None)
            );
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains("Client options are missing required values", exception.Message);
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_DiscoveryNull()
        {
            // Arrange
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, this.credentialsClientOptions);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync((Discovery.Discovery?)null);
            // Act
            var result = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_DiscoveryMissingClientSecretPost()
        {
            // Arrange
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, this.credentialsClientOptions);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            var discovery = new Discovery.Discovery(
                null,
                null,
                null,
                DateTimeOffset.MaxValue,
                ["code"],
                this.credentialsClientOptions.OidcProviderAddress,
                "/token",
                []
            );
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(discovery);
            // Act
            var result = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.logger.AssertLastItemContains(
                LogLevel.Error,
                "Provider does not support client_secret_post auth method"
            );
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_GetTokenFailure()
        {
            // Arrange
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, this.credentialsClientOptions);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
            };
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(this.discovery);
            // Act
            var result = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.logger.AssertLastItemContains(LogLevel.Error, "Unable to obtain token from");
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_GetTokenReturnsNull()
        {
            // Arrange
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, this.credentialsClientOptions);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(null),
                StatusCode = HttpStatusCode.OK,
            };
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(this.discovery);
            // Act
            var result = await clientCredentialsResolver.GetClientToken(this.clientName, CancellationToken.None);
            // Assert
            Assert.IsNull(result);
            this.logger.AssertLastItemContains(LogLevel.Error, "No token returned from");
        }

        [TestMethod]
        public async Task ClientCredentialsResolver_ClientAsParameter()
        {
            // Arrange
            var clientCredentialsResolver = new ClientCredentialsResolver(
                this.mockAsyncLock.Object,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(this.tokenResponse),
                StatusCode = HttpStatusCode.OK,
            };
            var credentialsClientOptions = new CredentialsClientOptions
            {
                ClientId = this.clientName,
                ClientSecret = "TopSecret",
                OidcProviderAddress = "https://localhost",
                Scope = "scope1",
            };
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(this.discovery);
            // Act 1 (uncached)
            var result1 = await clientCredentialsResolver.GetClientToken(
                this.clientName,
                credentialsClientOptions,
                CancellationToken.None
            );
            // Assert 1
            Assert.IsNotNull(result1);
            Assert.AreEqual("accesstoken", result1);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            this.mockDiscoveryResolver.VerifyAll();
            this.mockHttpClientFactory.VerifyAll();
            // Act 2 (cached)
            var result2 = await clientCredentialsResolver.GetClientToken(
                this.clientName,
                credentialsClientOptions,
                CancellationToken.None
            );
            // Assert 2
            this.mockUtcNow.VerifyAll();
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            Assert.IsNotNull(result2);
            Assert.AreEqual("accesstoken", result2);
            // Act 3 (cleared cache)
            await clientCredentialsResolver.ClearClientToken(this.clientName);
            var result3 = await clientCredentialsResolver.GetClientToken(
                this.clientName,
                credentialsClientOptions,
                CancellationToken.None
            );
            // Assert 3
            Assert.AreEqual(2, this.testMessageHandler.SendCount);
            Assert.IsNotNull(result3);
            Assert.AreEqual("accesstoken", result3);
            // Act 4 (expired)
            this.mockUtcNow.Setup(m => m.UtcNow).Returns(this.utcNow.AddHours(1));
            var result4 = await clientCredentialsResolver.GetClientToken(
                this.clientName,
                credentialsClientOptions,
                CancellationToken.None
            );
            Assert.AreEqual(3, this.testMessageHandler.SendCount);
            Assert.IsNotNull(result4);
            Assert.AreEqual("accesstoken", result4);
        }

        [TestMethod]
        public void ClientCredentialsResolver_LocksWhileResolving()
        {
            // Arrange
            this.clientOptions.CredentialsClientsOptions.TryAdd(this.clientName, this.credentialsClientOptions);
            var asyncLock = new AsyncLock<ClientCredentialsResolver>();
            Assert.IsFalse(asyncLock.IsLocked);
            var clientCredentialsResolver = new ClientCredentialsResolver(
                asyncLock,
                this.clientOptions,
                this.mockDiscoveryResolver.Object,
                this.mockHttpClientFactory.Object,
                this.logger,
                this.mockServiceProvider.Object,
                this.mockUtcNow.Object
            );
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage
            {
                Content = new TestJsonContent(this.tokenResponse),
                StatusCode = HttpStatusCode.OK,
            };
            Assert.IsNotNull(this.credentialsClientOptions.OidcProviderAddress);
            this.mockDiscoveryResolver.Setup(m =>
                    m.GetDiscovery(
                        this.clientOptions.DiscoveryOptions,
                        this.credentialsClientOptions.OidcProviderAddress,
                        CancellationToken.None
                    )
                )
                .ReturnsAsync(this.discovery);
            var blockerStarted = new AutoResetEvent(false);
            var blockerFinished = new AutoResetEvent(false);
            // Lock the AsyncLock for 15 seconds, order until told to release
            var blocker = Task.Run(() =>
            {
                var waiterRelease = asyncLock.Lock();
                blockerStarted.Set();
                blockerFinished.WaitOne(15000);
                waiterRelease.Dispose();
            });
            WaitHandle.WaitAll([blockerStarted]);
            // Request a tokens on a separate threads - these will wait until the lock is released
            string? backgroundToken1 = null;
            var task1Started = new AutoResetEvent(false);
            var tokenTask1 = Task.Run(async () =>
            {
                task1Started.Set();
                backgroundToken1 = await clientCredentialsResolver.GetClientToken(
                    this.clientName,
                    CancellationToken.None
                );
            });
            string? backgroundToken2 = null;
            var task2Started = new AutoResetEvent(false);
            var tokenTask2 = Task.Run(async () =>
            {
                task2Started.Set();
                backgroundToken2 = await clientCredentialsResolver.GetClientToken(
                    this.clientName,
                    CancellationToken.None
                );
            });
            WaitHandle.WaitAll([task1Started, task2Started]);
            // Release the lock to allow the token requests to proceed
            blockerFinished.Set();
            // Assert (both token requests complete successfully, only 1 token request made)
            Task.WaitAll([blocker, tokenTask1, tokenTask2]);
            Assert.AreEqual("accesstoken", backgroundToken1);
            Assert.AreEqual("accesstoken", backgroundToken2);
            Assert.AreEqual(1, this.testMessageHandler.SendCount);
            this.mockDiscoveryResolver.VerifyAll();
            this.mockHttpClientFactory.VerifyAll();
        }
    }
}
