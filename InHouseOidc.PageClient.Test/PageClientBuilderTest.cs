// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.PageClient.Resolver;
using InHouseOidc.PageClient.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.PageClient.Test
{
    [TestClass]
    public class PageClientBuilderTest
    {
        private TestServiceCollection serviceCollection = [];

        [TestInitialize]
        public void Initialise()
        {
            this.serviceCollection = [];
            var configuration = new Mock<IConfiguration>();
            this.serviceCollection.AddSingleton(configuration.Object);
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(m => m.Path).Returns("Authentication");
            configurationSection.Setup(m => m.Key).Returns("Authentication");
            configurationSection.Setup(m => m.Value).Returns((string?)null);
            configuration.Setup(m => m.GetSection("Authentication")).Returns(configurationSection.Object);
        }

        [TestMethod]
        public void ClientBuilder_AddPageApiClient()
        {
            // Arrange
            var clientName = "PageApiClient";
            // Act
            var clientBuilder = this.serviceCollection.AddOidcPageClient().AddApiClient(clientName);
            var duplicateException = Assert.ThrowsException<ArgumentException>(
                () => clientBuilder.AddApiClient(clientName)
            );
            clientBuilder.Build();
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            // Assert
            _ = serviceProvider.GetRequiredService<IPageAccessTokenResolver>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(clientName);
            Assert.IsNotNull(httpClient);
            Assert.IsNotNull(duplicateException);
            Assert.IsTrue(duplicateException.Message.Contains("Duplicate client name"));
        }

        [TestMethod]
        public void ClientBuilder_AddOidcPageClient()
        {
            // Arrange
            var pageClientOptions = new PageClientOptions
            {
                ClientId = "PageClientId",
                IssueLocalAuthenticationCookie = true,
                OidcProviderAddress = "https://localhost",
                Scope = "scope1",
                UniqueClaimMappings = new() { { "TestClaim", "TestClaim" } },
            };
            // Act
            var clientBuilder = this.serviceCollection.AddOidcPageClient().AddClient(pageClientOptions);
            var singleOnlyException = Assert.ThrowsException<ArgumentException>(
                () => clientBuilder.AddClient(pageClientOptions)
            );
            clientBuilder.Build();
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            // Assert
            _ = serviceProvider.GetRequiredService<IAuthenticationService>();
            _ = serviceProvider.GetRequiredService<CookieAuthenticationHandler>();
            _ = serviceProvider.GetRequiredService<OpenIdConnectHandler>();
            Assert.IsNotNull(singleOnlyException);
            Assert.IsTrue(singleOnlyException.Message.Contains("AddOidcPageClient can only be called once"));
            // Assert authentication options
            var authenticationOptions = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();
            Assert.AreEqual(
                PageConstant.AuthenticationSchemeCookie,
                authenticationOptions.Value.DefaultAuthenticateScheme
            );
            Assert.AreEqual(
                OpenIdConnectDefaults.AuthenticationScheme,
                authenticationOptions.Value.DefaultChallengeScheme
            );
            // Assert cookie options
            var cookieOptionsMonitor = serviceProvider.GetRequiredService<
                IOptionsMonitor<CookieAuthenticationOptions>
            >();
            var cookieOptions = cookieOptionsMonitor.Get(PageConstant.AuthenticationSchemeCookie);
            Assert.AreEqual(pageClientOptions.ClientId, cookieOptions.Cookie.Name);
            // Assert OpenID Connect options
            var openIdConnectOptionsMonitor = serviceProvider.GetRequiredService<
                IOptionsMonitor<OpenIdConnectOptions>
            >();
            var openIdConnectOptions = openIdConnectOptionsMonitor.Get("OpenIdConnect");
            Assert.AreEqual(pageClientOptions.OidcProviderAddress, openIdConnectOptions.Authority);
        }

        [TestMethod]
        public void ClientBuilder_SetAll()
        {
            // Arrange
            var discoveryCacheTime = TimeSpan.FromHours(1);
            var internalHttpClientName = "InHouseOidc";
            var maxRetryAttempts = 10;
            var retryDelayMilliseconds = 25;
            // Act
            var pageClientBuilder = this
                .serviceCollection.AddOidcPageClient()
                .EnableDiscoveryGrantTypeValidation(false)
                .EnableDiscoveryIssuerValidation(false)
                .SetDiscoveryCacheTime(discoveryCacheTime)
                .SetInternalHttpClientName(internalHttpClientName)
                .SetMaxRetryAttempts(maxRetryAttempts)
                .SetRetryDelayMilliseconds(retryDelayMilliseconds);
            pageClientBuilder.Build();
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            // Assert
            var clientOptions = serviceProvider.GetRequiredService<ClientOptions>();
            Assert.AreEqual(discoveryCacheTime, clientOptions.DiscoveryOptions.CacheTime);
            Assert.AreEqual(internalHttpClientName, clientOptions.InternalHttpClientName);
            Assert.AreEqual(maxRetryAttempts, clientOptions.MaxRetryAttempts);
            Assert.AreEqual(retryDelayMilliseconds, clientOptions.RetryDelayMilliseconds);
            Assert.IsFalse(clientOptions.DiscoveryOptions.ValidateGrantTypes);
            Assert.IsFalse(clientOptions.DiscoveryOptions.ValidateIssuer);
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(internalHttpClientName);
            Assert.IsNotNull(httpClient);
        }
    }
}
