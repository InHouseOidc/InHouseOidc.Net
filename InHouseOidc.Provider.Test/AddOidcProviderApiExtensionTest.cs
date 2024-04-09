// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Provider.Test
{
    [TestClass]
    public class AddOidcProviderApiExtensionTest
    {
        [TestMethod]
        public void AddOidcProviderApi()
        {
            // Arrange
            var audience = "localapi";
            var scope = "localscope";
            var scopes = new[] { scope };
            var serviceCollection = new TestServiceCollection();
            // Act
            serviceCollection.AddOidcProviderApi(audience, scopes);
            // Assert
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _ = serviceProvider.GetRequiredService<IAuthenticationService>();
            var authenticationOptions = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();
            Assert.AreEqual(ApiConstant.AuthenticationScheme, authenticationOptions.Value.Schemes.Single().Name);
            var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>();
            var policy = authorizationOptions.Value.GetPolicy(scope);
            Assert.IsNotNull(policy);
            CollectionAssert.Contains(policy.AuthenticationSchemes.ToList(), ApiConstant.AuthenticationScheme);
        }
    }
}
