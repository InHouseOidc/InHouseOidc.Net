// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.Provider
{
    /// <summary>
    /// AddOidcProviderApi extension methods.
    /// </summary>
    public static class AddOidcProviderApiExtension
    {
        /// <summary>
        /// Adds an API implemented in the OIDC Provider.<br />
        /// </summary>
        /// <param name="serviceCollection">The ServiceCollection being configured during startup.</param>
        /// <param name="audience">The audience name for the API resource.</param>
        /// <param name="scopes">One or more valid scopes related to the audience in the OIDC Provider.</param>
        /// <returns><see cref="IServiceCollection"/> so additional calls can be chained.</returns>
        public static IServiceCollection AddOidcProviderApi(
            this IServiceCollection serviceCollection,
            string audience,
            params string[] scopes
        )
        {
            serviceCollection.AddAuthentication(authenticationOptions =>
            {
                authenticationOptions.AddScheme<ApiAuthenticationHandler>(ApiConstant.AuthenticationScheme, null);
            });
            serviceCollection.AddSingleton(
                new ApiAuthenticationOptions { Audience = audience, Scopes = scopes.ToList() }
            );
            serviceCollection.AddAuthorization(authorizationOptions =>
            {
                foreach (var scope in scopes)
                {
                    // Check the request is authenticated and has the required scope claim
                    authorizationOptions.AddProviderPolicyScope(ApiConstant.AuthenticationScheme, scope);
                }
            });
            return serviceCollection;
        }
    }
}
