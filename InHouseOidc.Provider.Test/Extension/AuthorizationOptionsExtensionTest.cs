// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Extension;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Provider.Test
{
    [TestClass]
    public class AuthorizationOptionsExtensionTest
    {
        [TestMethod]
        public async Task AddApiPolicyScope()
        {
            // Arrange 1
            var authorizationOptions = new AuthorizationOptions();
            var scheme = "testscheme";
            var scope = "testscope";
            // Act 1
            authorizationOptions.AddProviderPolicyScope(scheme, scope);
            // Assert 1
            var policyScheme = authorizationOptions.GetPolicy(scope);
            Assert.IsNotNull(policyScheme);
            CollectionAssert.Contains(policyScheme.AuthenticationSchemes.ToList(), scheme);
            Assert.AreEqual(2, policyScheme.Requirements.Count);
            CollectionAssert.Contains(
                policyScheme.Requirements.Select(r => r.GetType()).ToList(),
                typeof(DenyAnonymousAuthorizationRequirement)
            );
            CollectionAssert.Contains(
                policyScheme.Requirements.Select(r => r.GetType()).ToList(),
                typeof(AssertionRequirement)
            );
            // Arrange 2
            var claims = new List<Claim> { new(JsonWebTokenClaim.Scope, "ignorescope testscope") };
            var claimsIdentity = new ClaimsIdentity(claims, scheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var context = new AuthorizationHandlerContext(policyScheme.Requirements, claimsPrincipal, null);
            var requirement =
                policyScheme.Requirements.Single(r => r.GetType() == typeof(AssertionRequirement))
                as AssertionRequirement;
            Assert.IsNotNull(requirement);
            // Act 2
            var result = await requirement.Handler(context);
            // Assert 2
            Assert.IsTrue(result);
        }
    }
}
