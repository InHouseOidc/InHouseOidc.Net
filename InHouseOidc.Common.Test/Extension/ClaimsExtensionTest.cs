// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Security.Claims;

namespace InHouseOidc.Common.Test.Extension
{
    [TestClass]
    public class ClaimsExtensionTest
    {
        [DataTestMethod]
        [DynamicData(nameof(HasScope_Data))]
        public void HasScope(IEnumerable<Claim> claims, string scope, bool expectedResult)
        {
            // Act
            var result = claims.HasScope(scope);
            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        public static IEnumerable<object[]> HasScope_Data
        {
            get
            {
                return new[]
                {
                    new object[] { new List<Claim>(), "scope1", false },
                    new object[]
                    {
                        new List<Claim>
                        {
                            new Claim(JsonWebTokenClaim.Scope, "scope1"),
                            new Claim(JsonWebTokenClaim.Scope, "scope2"),
                        },
                        "scope3",
                        false,
                    },
                    new object[]
                    {
                        new List<Claim>
                        {
                            new Claim(JsonWebTokenClaim.Scope, "scope1"),
                            new Claim(JsonWebTokenClaim.Scope, "scope2"),
                        },
                        "scope2",
                        true,
                    },
                    new object[]
                    {
                        new List<Claim> { new Claim(JsonWebTokenClaim.Scope, "scope1 scope2") },
                        "scope3",
                        false,
                    },
                    new object[]
                    {
                        new List<Claim> { new Claim(JsonWebTokenClaim.Scope, "scope1 scope2") },
                        "scope1",
                        true,
                    },
                    new object[]
                    {
                        new List<Claim> { new Claim(JsonWebTokenClaim.Scope, "scope1") },
                        "scope2",
                        false,
                    },
                    new object[]
                    {
                        new List<Claim> { new Claim(JsonWebTokenClaim.Scope, "scope1") },
                        "scope1",
                        true,
                    },
                };
            }
        }
    }
}
