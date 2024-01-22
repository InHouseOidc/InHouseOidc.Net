// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InHouseOidc.Api.Test
{
    [TestClass]
    public class AddOidcApiExtensionTest
    {
        [TestMethod]
        public void AddOidcApi_ServicesAddedAndConfigured()
        {
            // Arrange
            var providerAddress = "https://localhost";
            var audience = "localapi";
            var scope = "localscope";
            var scopes = new List<string>() { scope };
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton(new Mock<ILoggerFactory>().Object);
            serviceCollection.AddSingleton(new Mock<ILogger<DefaultAuthorizationService>>().Object);
            var configuration = new Mock<IConfiguration>();
            serviceCollection.AddSingleton(configuration.Object);
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(m => m.Path).Returns("Authentication");
            configurationSection.Setup(m => m.Key).Returns("Authentication");
            configurationSection.Setup(m => m.Value).Returns((string?)null);
            configuration.Setup(m => m.GetSection("Authentication")).Returns(configurationSection.Object);
            // Act
            serviceCollection.AddOidcApi(providerAddress, audience, scopes);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Assert
            Assert.IsNotNull(serviceProvider);
            _ = serviceProvider.GetRequiredService<IAuthenticationService>();
            _ = serviceProvider.GetRequiredService<JwtBearerHandler>();
            _ = serviceProvider.GetRequiredService<IAuthorizationService>();
            var authenticationOptions = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();
            Assert.AreEqual(
                JwtBearerDefaults.AuthenticationScheme,
                authenticationOptions.Value.DefaultAuthenticateScheme
            );
            Assert.AreEqual(JwtBearerDefaults.AuthenticationScheme, authenticationOptions.Value.DefaultChallengeScheme);
            var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var jwtBearerOptions = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
            Assert.AreEqual(providerAddress, jwtBearerOptions.Authority);
            var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>();
            Assert.IsNotNull(authorizationOptions.Value.GetPolicy(scope));
        }
    }
}
