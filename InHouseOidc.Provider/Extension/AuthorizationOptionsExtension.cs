// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using Microsoft.AspNetCore.Authorization;

namespace InHouseOidc.Provider.Extension
{
    public static class AuthorizationOptionsExtension
    {
        public static void AddProviderPolicyScope(
            this AuthorizationOptions authorizationOptions,
            string scheme,
            string scope
        )
        {
            authorizationOptions.AddPolicy(
                scope,
                authorizationPolicyBuilder =>
                {
                    authorizationPolicyBuilder.AddAuthenticationSchemes(scheme);
                    authorizationPolicyBuilder.RequireAuthenticatedUser();
                    authorizationPolicyBuilder.RequireAssertion(authorizationHandlerContext =>
                    {
                        return authorizationHandlerContext.User.Claims.HasScope(scope);
                    });
                }
            );
        }
    }
}
