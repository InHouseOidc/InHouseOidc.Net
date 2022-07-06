﻿// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace InHouseOidc.Provider.Test
{
    [TestClass]
    public class ProviderBuilderTest
    {
        [TestMethod]
        public void Build_Success()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var mockClientStore = new Mock<IClientStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockClientStore.Object);
            var mockResourceStore = new Mock<IResourceStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockResourceStore.Object);
            // Act
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IAuthenticationService>();
            var authenticationOptions = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();
            Assert.AreEqual(
                CookieAuthenticationDefaults.AuthenticationScheme,
                authenticationOptions.Value.DefaultAuthenticateScheme
            );
            _ = serviceProvider.GetRequiredService<CookieAuthenticationHandler>();
            var cookieOptionsMonitor = serviceProvider.GetRequiredService<
                IOptionsMonitor<CookieAuthenticationOptions>
            >();
            var cookieOptions = cookieOptionsMonitor.Get("Cookies");
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(providerOptions.AuthenticationCookieName, cookieOptions.Cookie.Name);
            _ = serviceProvider.GetRequiredService<ProviderAuthenticationHandler>();
        }

        [TestMethod]
        public void Build_NoSigningKeys()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            // Act
            var exception = Assert.ThrowsException<InvalidOperationException>(() => providerBuilder.Build());
            // Assert
            Assert.IsNotNull(exception);
            StringAssert.Contains(exception.Message, ".SetSigningCertificates() is required");
        }

        [TestMethod]
        public void EnableAuthorizationCodeFlow()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var mockClientStore = new Mock<IClientStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockClientStore.Object);
            var mockCodeStore = new Mock<ICodeStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockCodeStore.Object);
            var mockResourceStore = new Mock<IResourceStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockResourceStore.Object);
            // Act
            providerBuilder.EnableAuthorizationCodeFlow(false);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IEndpointHandler<AuthorizationHandler>>();
            _ = serviceProvider.GetRequiredService<IEndpointHandler<EndSessionHandler>>();
            _ = serviceProvider.GetRequiredService<IProviderSession>();
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.IsFalse(providerOptions.AuthorizationCodePkceRequired);
            CollectionAssert.Contains(providerOptions.GrantTypes, GrantType.AuthorizationCode);
        }

        [TestMethod]
        public void EnableCheckSessionEndpoint()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            // Act
            providerBuilder.EnableCheckSessionEndpoint();
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IEndpointHandler<CheckSessionHandler>>();
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.IsTrue(providerOptions.CheckSessionEndpointEnabled);
        }

        [TestMethod]
        public void EnableClientCredentialsFlow()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var mockResourceStore = new Mock<IResourceStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockResourceStore.Object);
            // Act
            providerBuilder.EnableClientCredentialsFlow();
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IProviderToken>();
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            CollectionAssert.Contains(providerOptions.GrantTypes, GrantType.ClientCredentials);
        }

        [TestMethod]
        public void EnableRefreshTokenGrant()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            // Act
            providerBuilder.EnableRefreshTokenGrant();
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            CollectionAssert.Contains(providerOptions.GrantTypes, GrantType.RefreshToken);
        }

        [TestMethod]
        public void EnableUserInfoEndpoint()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var mockClientStore = new Mock<IClientStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockClientStore.Object);
            var mockUserStore = new Mock<IUserStore>(MockBehavior.Strict);
            serviceCollection.AddSingleton(mockUserStore.Object);
            // Act
            providerBuilder.EnableUserInfoEndpoint();
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IEndpointHandler<UserInfoHandler>>();
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.IsTrue(providerOptions.UserInfoEndpointEnabled);
        }

        [TestMethod]
        public void LogFailuresAsInformation()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            // Act
            providerBuilder.LogFailuresAsInformation(false);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.IsFalse(providerOptions.LogFailuresAsInformation);
        }

        [TestMethod]
        public void SetAuthorizationEndpointAddress()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var path = "/authorize";
            // Act
            providerBuilder.SetAuthorizationEndpointAddress(path);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(new Uri(path, UriKind.Relative), providerOptions.AuthorizationEndpointUri);
        }

        [TestMethod]
        public void SetAuthorizationEndpointUri()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var uri = new Uri("/authorize", UriKind.Relative);
            // Act
            providerBuilder.SetAuthorizationEndpointUri(uri);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(uri, providerOptions.AuthorizationEndpointUri);
        }

        [TestMethod]
        public void SetCheckSessionEndpointAddress()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var path = "/checksession";
            // Act
            providerBuilder.SetCheckSessionEndpointAddress(path);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(new Uri(path, UriKind.Relative), providerOptions.CheckSessionEndpointUri);
        }

        [TestMethod]
        public void SetCheckSessionEndpointUri()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var uri = new Uri("/checksession", UriKind.Relative);
            // Act
            providerBuilder.SetCheckSessionEndpointUri(uri);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(uri, providerOptions.CheckSessionEndpointUri);
        }

        [TestMethod]
        public void SetEndSessionEndpointAddress()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var path = "/endsession";
            // Act
            providerBuilder.SetEndSessionEndpointAddress(path);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(new Uri(path, UriKind.Relative), providerOptions.EndSessionEndpointUri);
        }

        [TestMethod]
        public void SetEndSessionEndpointUri()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var uri = new Uri("/endsession", UriKind.Relative);
            // Act
            providerBuilder.SetEndSessionEndpointUri(uri);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(uri, providerOptions.EndSessionEndpointUri);
        }

        [TestMethod]
        public void SetIdentityProvider()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var identityProvider = "unittest";
            // Act
            providerBuilder.SetIdentityProvider(identityProvider);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(identityProvider, providerOptions.IdentityProvider);
        }

        [TestMethod]
        public void SetSigningCertificates_NoPrivateKey()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            // Act
            var exception = Assert.ThrowsException<ArgumentException>(
                () =>
                    providerBuilder.SetSigningCertificates(
                        new[] { TestCertificate.CreatePublicOnly(DateTimeOffset.UtcNow) }
                    )
            );
            // Assert
            StringAssert.Contains(exception.Message, "must include a private key");
        }

        [TestMethod]
        public void SetSigningCertificates_NotRS256()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            // Act
            var exception = Assert.ThrowsException<ArgumentException>(
                () =>
                    providerBuilder.SetSigningCertificates(
                        new[] { TestCertificate.CreateNonRS256(DateTimeOffset.UtcNow) }
                    )
            );
            // Assert
            StringAssert.Contains(exception.Message, "must support RS256 algorithm");
        }

        [TestMethod]
        public void SetSigningCertificates_Success()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            // Act
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(1, providerOptions.SigningKeys.Count);
        }

        [TestMethod]
        public void SetTokenEndpointAddress()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var path = "/token";
            // Act
            providerBuilder.SetTokenEndpointAddress(path);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(new Uri(path, UriKind.Relative), providerOptions.TokenEndpointUri);
        }

        [TestMethod]
        public void SetTokenEndpointUri()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var uri = new Uri("/token", UriKind.Relative);
            // Act
            providerBuilder.SetTokenEndpointUri(uri);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(uri, providerOptions.TokenEndpointUri);
        }

        [TestMethod]
        public void SetUserInfoEndpointAddress()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var path = "/userinfo";
            // Act
            providerBuilder.SetUserInfoEndpointAddress(path);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(new Uri(path, UriKind.Relative), providerOptions.UserInfoEndpointUri);
        }

        [TestMethod]
        public void SetUserInfoEndpointUri()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            var uri = new Uri("/userinfo", UriKind.Relative);
            // Act
            providerBuilder.SetUserInfoEndpointUri(uri);
            providerBuilder.Build();
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.IsNotNull(serviceProvider);
            var providerOptions = serviceProvider.GetRequiredService<ProviderOptions>();
            Assert.AreEqual(uri, providerOptions.UserInfoEndpointUri);
        }

        [TestMethod]
        public void SetUserInfoEndpointUri_Invalid()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var providerBuilder = serviceCollection.AddOidcProvider();
            providerBuilder.SetSigningCertificates(new[] { TestCertificate.Create(DateTimeOffset.UtcNow) });
            // Act/Assert 1
            var exception1 = Assert.ThrowsException<ArgumentException>(
                () => providerBuilder.SetUserInfoEndpointUri(new Uri("%2c%/userinfo", UriKind.Relative))
            );
            StringAssert.Contains(exception1.Message, "Invalid URI");
            // Act/Assert 2
            var exception2 = Assert.ThrowsException<ArgumentException>(
                () => providerBuilder.SetUserInfoEndpointUri(new Uri("~/userinfo", UriKind.Relative))
            );
            StringAssert.Contains(exception2.Message, "Invalid URI");
            // Act/Assert 3
            var exception3 = Assert.ThrowsException<ArgumentException>(
                () => providerBuilder.SetUserInfoEndpointUri(new Uri("https://localhost/userinfo", UriKind.Absolute))
            );
            StringAssert.Contains(exception3.Message, "Invalid URI");
        }
    }
}
