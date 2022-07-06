// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.PageClient.Resolver;
using InHouseOidc.PageClient.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;

namespace InHouseOidc.PageClient.Test
{
    [TestClass]
    public class PageClientBuilderTest
    {
        private TestServiceCollection serviceCollection = new();

        [TestInitialize]
        public void Initialise()
        {
            this.serviceCollection = new();
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
                CookieAuthenticationDefaults.AuthenticationScheme,
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
            var cookieOptions = cookieOptionsMonitor.Get("Cookies");
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
            var pageClientBuilder = this.serviceCollection
                .AddOidcPageClient()
                .SetDiscoveryCacheTime(discoveryCacheTime)
                .SetInternalHttpClientName(internalHttpClientName)
                .SetMaxRetryAttempts(maxRetryAttempts)
                .SetRetryDelayMilliseconds(retryDelayMilliseconds);
            pageClientBuilder.Build();
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            // Assert
            var clientOptions = serviceProvider.GetRequiredService<ClientOptions>();
            Assert.AreEqual(discoveryCacheTime, clientOptions.DiscoveryCacheTime);
            Assert.AreEqual(internalHttpClientName, clientOptions.InternalHttpClientName);
            Assert.AreEqual(maxRetryAttempts, clientOptions.MaxRetryAttempts);
            Assert.AreEqual(retryDelayMilliseconds, clientOptions.RetryDelayMilliseconds);
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(internalHttpClientName);
            Assert.IsNotNull(httpClient);
        }
    }
}
