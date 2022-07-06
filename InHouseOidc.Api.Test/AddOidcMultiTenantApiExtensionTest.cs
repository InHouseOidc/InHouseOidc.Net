// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace InHouseOidc.Api.Test
{
    [TestClass]
    public class AddOidcMultiTenantApiExtensionTest
    {
        [TestMethod]
        public void AddOidcMultiTenantApi_ServicesAddedAndConfigured()
        {
            // Arrange
            var tenant1 = "testapi-1.test";
            var tenant2 = "testapi-2.test";
            var providerAddress1 = "https://provider-1.test";
            var providerAddress2 = "https://provider-2.test";
            var tenantProviders = new Dictionary<string, string>
            {
                { tenant1, providerAddress1 },
                { tenant2, providerAddress2 },
            };
            var audience = "tenantapi";
            var scope = "tenantscope";
            var scopes = new List<string> { scope };
            var serviceCollection = new TestServiceCollection();
            // Act
            serviceCollection.AddOidcMultiTenantApi(audience, tenantProviders, scopes);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            // Assert services
            Assert.IsNotNull(serviceProvider);
            serviceCollection.AssertContains(ServiceLifetime.Scoped, typeof(IAuthenticationService));
            serviceCollection.AssertContains(ServiceLifetime.Transient, typeof(JwtBearerHandler));
            serviceCollection.AssertContains(ServiceLifetime.Transient, typeof(IAuthorizationService));
            // Assert authentication options
            var authenticationOptions = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();
            Assert.AreEqual(
                ApiConstant.MultiTenantAuthenticationScheme,
                authenticationOptions.Value.DefaultAuthenticateScheme
            );
            Assert.AreEqual(
                ApiConstant.MultiTenantAuthenticationScheme,
                authenticationOptions.Value.DefaultChallengeScheme
            );
            // Assert policy scheme to forward to JWT schemes
            var optionsMonitorPolicy = serviceProvider.GetRequiredService<IOptionsMonitor<PolicySchemeOptions>>();
            var policySchemeOptions = optionsMonitorPolicy.Get(ApiConstant.MultiTenantAuthenticationScheme);
            Assert.IsNotNull(policySchemeOptions);
            Assert.IsNotNull(policySchemeOptions.ForwardDefaultSelector);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Host = tenant1;
            var forwardedScheme = policySchemeOptions.ForwardDefaultSelector(httpContext);
            Assert.AreEqual(tenant1, forwardedScheme);
            // Assert JWT options
            var optionsMonitorJwt = serviceProvider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
            var tenant1JwtBearerOptions = optionsMonitorJwt.Get(tenant1);
            Assert.IsNotNull(tenant1JwtBearerOptions);
            Assert.AreEqual(providerAddress1, tenant1JwtBearerOptions.Authority);
            var tenant2JwtBearerOptions = optionsMonitorJwt.Get(tenant2);
            Assert.IsNotNull(tenant2JwtBearerOptions);
            Assert.AreEqual(providerAddress2, tenant2JwtBearerOptions.Authority);
            // Assert authorization options
            var authorizationOptions = serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>();
            Assert.IsNotNull(authorizationOptions.Value.GetPolicy(scope));
        }
    }
}
