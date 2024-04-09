// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.Api
{
    public static class AddOidcApiExtension
    {
        /// <summary>
        /// Adds OIDC API JWT authentication and authorisation.<br />
        /// Use scope names to secure endpoints, e.g. [Authorize(Policy = "scope")].
        /// </summary>
        /// <param name="serviceCollection">The ServiceCollection being configured during startup.</param>
        /// <param name="providerAddress">The OIDC Provider address.</param>
        /// <param name="audience">The audience name for the API resource.</param>
        /// <param name="scopes">One or more valid scopes related to the audience in the OIDC Provider.</param>
        /// <returns><see cref="IServiceCollection"/> so additional calls can be chained.</returns>
        public static IServiceCollection AddOidcApi(
            this IServiceCollection serviceCollection,
            string providerAddress,
            string audience,
            List<string> scopes
        )
        {
            serviceCollection
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidateIssuer = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidAudiences = [audience],
                        ValidIssuer = providerAddress,
                    };
                    options.RequireHttpsMetadata = providerAddress.StartsWith("https://");
                    options.Authority = providerAddress;
                });
            serviceCollection.AddAuthorization(authorizationOptions =>
            {
                foreach (var scope in scopes)
                {
                    // Check the request is authenticated and has the required scope claim
                    authorizationOptions.AddApiPolicyScope(JwtBearerDefaults.AuthenticationScheme, scope);
                }
            });
            return serviceCollection;
        }
    }
}
