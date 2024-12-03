// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Resolver;
using InHouseOidc.Bff.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InHouseOidc.Bff.Test
{
    [TestClass]
    public class BffBuilderTest
    {
        private static readonly string ApiClientName = "BffApiClient";
        private static readonly string CallbackPath = "/test/callback";
        private static readonly string ClientId = "BffClientId";
        private static readonly string ClientSecret = "topsecret";
        private static readonly string CookieName = "test.cookie";
        private static readonly string Hostname = "localhost";
        private static readonly string LoginPath = "/test/login";
        private static readonly string LogoutPath = "/test/logout";
        private static readonly string NameClaimType = "testname";
        private static readonly string OidcProviderAddress = "https://localhost";
        private static readonly string PostLogoutRedirectAddress = "https://logged-out.com";
        private static readonly string RoleClaimType = "testrole";
        private static readonly string Scope = "testscope";
        private static readonly string UserInfoPath = "/test/userinfo";

        private readonly BffClientOptions bffClientOptions =
            new()
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                OidcProviderAddress = OidcProviderAddress,
                Scope = Scope,
            };
        private readonly Dictionary<string, string> uniqueClaimMappings = new() { { "TestClaim", "TestClaim" } };

        private TestServiceCollection serviceCollection = [];

        [TestInitialize]
        public void Initialise()
        {
            this.serviceCollection = [];
        }

        [TestMethod]
        public void AddApiClient_Duplicate()
        {
            // Act
            var mixedException = Assert.ThrowsException<ArgumentException>(
                () => this.serviceCollection.AddOidcBff().AddApiClient(ApiClientName).AddApiClient(ApiClientName)
            );
            // Assert
            Assert.IsNotNull(mixedException);
            Assert.AreEqual("Duplicate client name: BffApiClient (Parameter 'clientName')", mixedException.Message);
        }

        [TestMethod]
        public void AddApiClient_Success()
        {
            // Act
            this.serviceCollection.AddOidcBff().AddApiClient(ApiClientName).Build();
            // Assert
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IBffAccessTokenResolver>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(ApiClientName);
            Assert.IsNotNull(httpClient);
        }

        [TestMethod]
        public void AddMultitenantOidcClients_MixedClients_Invalid()
        {
            // Arrange
            Dictionary<string, BffClientOptions> bffHostClientOptions =
                new() { { "localhost", this.bffClientOptions } };
            // Act
            var mixedException = Assert.ThrowsException<InvalidOperationException>(
                () =>
                    this
                        .serviceCollection.AddOidcBff()
                        .SetOidcClient(this.bffClientOptions)
                        .AddMultitenantOidcClients(bffHostClientOptions)
            );
            // Assert
            Assert.IsNotNull(mixedException);
            Assert.AreEqual("Use either AddMultitenantOidcClients or SetOidcClient, not both", mixedException.Message);
        }

        [TestMethod]
        public void Build_AuthorizationPolicy_Setup()
        {
            // Arrange
            var bffBuilder = this.serviceCollection.AddOidcBff();
            // Act
            bffBuilder.Build();
            // Assert
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
            var policy = authorizationOptions.GetPolicy(BffConstant.BffApiPolicy);
            Assert.IsNotNull(policy);
            CollectionAssert.Contains(policy.AuthenticationSchemes.ToList(), BffConstant.AuthenticationSchemeCookie);
        }

        [DataTestMethod]
        [DataRow(null, 2)]
        [DataRow("", 2)]
        [DataRow("testscope", 3)]
        public void Build_MultiTenant_Success(string? scope, int expectedScopeCount)
        {
            // Arrange
            var bffClientOptions = new BffClientOptions
            {
                ClientId = ClientId,
                ClientSecret = ClientSecret,
                OidcProviderAddress = OidcProviderAddress,
                Scope = scope,
            };
            Dictionary<string, BffClientOptions> bffHostClientOptions = new() { { Hostname, bffClientOptions } };
            var bffBuilder = this.serviceCollection.AddOidcBff().AddMultitenantOidcClients(bffHostClientOptions);
            // Act
            bffBuilder.Build();
            // Assert authentication setup
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IAuthenticationService>();
            _ = serviceProvider.GetRequiredService<CookieAuthenticationHandler>();
            _ = serviceProvider.GetRequiredService<OpenIdConnectHandler>();
            var cookieOptionsMonitor = serviceProvider.GetRequiredService<
                IOptionsMonitor<CookieAuthenticationOptions>
            >();
            var cookieOptions = cookieOptionsMonitor.Get(BffConstant.AuthenticationSchemeCookie);
            var clientOptions = serviceProvider.GetRequiredService<ClientOptions>();
            Assert.AreEqual(clientOptions.AuthenticationCookieName, cookieOptions.Cookie.Name);
            // Assert OpenID Connect options
            var openIdConnectOptionsMonitor = serviceProvider.GetRequiredService<
                IOptionsMonitor<OpenIdConnectOptions>
            >();
            var openIdConnectOptions = openIdConnectOptionsMonitor.Get(Hostname);
            Assert.AreEqual(this.bffClientOptions.OidcProviderAddress, openIdConnectOptions.Authority);
            Assert.AreEqual(expectedScopeCount, openIdConnectOptions.Scope.Count); // "openid profile +?"
        }

        [TestMethod]
        public void Build_SingleTenant_Success()
        {
            // Arrange
            var bffBuilder = this
                .serviceCollection.AddOidcBff()
                .SetOidcClient(this.bffClientOptions)
                .SetUniqueClaimMappings(this.uniqueClaimMappings);
            // Act
            bffBuilder.Build();
            // Assert authentication setup
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IAuthenticationService>();
            _ = serviceProvider.GetRequiredService<CookieAuthenticationHandler>();
            _ = serviceProvider.GetRequiredService<OpenIdConnectHandler>();
            var cookieOptionsMonitor = serviceProvider.GetRequiredService<
                IOptionsMonitor<CookieAuthenticationOptions>
            >();
            var cookieOptions = cookieOptionsMonitor.Get(BffConstant.AuthenticationSchemeCookie);
            var clientOptions = serviceProvider.GetRequiredService<ClientOptions>();
            Assert.AreEqual(clientOptions.AuthenticationCookieName, cookieOptions.Cookie.Name);
            // Assert OpenID Connect options
            var openIdConnectOptionsMonitor = serviceProvider.GetRequiredService<
                IOptionsMonitor<OpenIdConnectOptions>
            >();
            var openIdConnectOptions = openIdConnectOptionsMonitor.Get(OpenIdConnectDefaults.AuthenticationScheme);
            Assert.AreEqual(this.bffClientOptions.OidcProviderAddress, openIdConnectOptions.Authority);
            Assert.AreEqual(3, openIdConnectOptions.Scope.Count); // "openid profile testscope"
        }

        [TestMethod]
        public void Build_SetAll()
        {
            // Arrange
            var discoveryCacheTime = TimeSpan.FromHours(1);
            var internalHttpClientName = "InHouseOidc";
            var maxRetryAttempts = 10;
            var retryDelayMilliseconds = 25;
            // Act
            var bffBuilder = this
                .serviceCollection.AddOidcBff()
                .EnableDiscoveryGrantTypeValidation(false)
                .EnableDiscoveryIssuerValidation(false)
                .EnableGetClaimsFromUserInfoEndpoint(false)
                .SetAuthenticationCookieName(CookieName)
                .SetCallbackPath(CallbackPath)
                .SetDiscoveryCacheTime(discoveryCacheTime)
                .SetInternalHttpClientName(internalHttpClientName)
                .SetLoginPath(LoginPath)
                .SetLogoutPath(LogoutPath)
                .SetMaxRetryAttempts(maxRetryAttempts)
                .SetNameClaimType(NameClaimType)
                .SetPostLogoutRedirectAddress(PostLogoutRedirectAddress)
                .SetRetryDelayMilliseconds(retryDelayMilliseconds)
                .SetRoleClaimType(RoleClaimType)
                .SetUserInfoPath(UserInfoPath);
            bffBuilder.Build();
            var serviceProvider = this.serviceCollection.BuildServiceProvider();
            // Assert
            var clientOptions = serviceProvider.GetRequiredService<ClientOptions>();
            Assert.IsFalse(clientOptions.DiscoveryOptions.ValidateGrantTypes);
            Assert.IsFalse(clientOptions.DiscoveryOptions.ValidateIssuer);
            Assert.IsFalse(clientOptions.GetClaimsFromUserInfoEndpoint);
            Assert.AreEqual(CookieName, clientOptions.AuthenticationCookieName);
            Assert.AreEqual(CallbackPath, clientOptions.CallbackPath);
            Assert.AreEqual(discoveryCacheTime, clientOptions.DiscoveryOptions.CacheTime);
            Assert.AreEqual(internalHttpClientName, clientOptions.InternalHttpClientName);
            Assert.AreEqual(LoginPath, clientOptions.LoginEndpointUri.OriginalString);
            Assert.AreEqual(LogoutPath, clientOptions.LogoutEndpointUri.OriginalString);
            Assert.AreEqual(maxRetryAttempts, clientOptions.MaxRetryAttempts);
            Assert.AreEqual(NameClaimType, clientOptions.NameClaimType);
            Assert.AreEqual(PostLogoutRedirectAddress, clientOptions.PostLogoutRedirectAddress);
            Assert.AreEqual(retryDelayMilliseconds, clientOptions.RetryDelayMilliseconds);
            Assert.AreEqual(RoleClaimType, clientOptions.RoleClaimType);
            Assert.AreEqual(UserInfoPath, clientOptions.UserInfoEndpointUri.OriginalString);
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(internalHttpClientName);
            Assert.IsNotNull(httpClient);
        }

        [TestMethod]
        public void SetOidcClient_MixedClients_Invalid()
        {
            // Arrange
            Dictionary<string, BffClientOptions> bffHostClientOptions =
                new() { { "localhost", this.bffClientOptions } };
            // Act
            var mixedException = Assert.ThrowsException<InvalidOperationException>(
                () =>
                    this
                        .serviceCollection.AddOidcBff()
                        .AddMultitenantOidcClients(bffHostClientOptions)
                        .SetOidcClient(this.bffClientOptions)
            );
            // Assert
            Assert.IsNotNull(mixedException);
            Assert.AreEqual("Use either SetOidcClient or AddMultitenantOidcClients, not both", mixedException.Message);
        }
    }
}
