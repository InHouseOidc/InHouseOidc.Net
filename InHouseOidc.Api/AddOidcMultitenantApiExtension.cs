// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.Api
{
    public static class AddOidcMultiTenantApiExtension
    {
        /// <summary>
        /// Adds OIDC API JWT authentication and authorisation for a host header base tenanted API.<br />
        /// Creates a policy per scope for securing endpoints, e.g. [Authorize(Policy = "scope")].
        /// </summary>
        /// <param name="serviceCollection">The ServiceCollection being configured during startup.</param>
        /// <param name="audience">The audience name for the API resource.</param>
        /// <param name="tenantProviders">The list of tuples of tenant hostnames + provider addresses to authenticate.</param>
        /// <param name="scopes">One or more valid scopes related to the audience in the OIDC Provider.</param>
        /// <returns><see cref="IServiceCollection"/> so additional calls can be chained.</returns>
        public static IServiceCollection AddOidcMultiTenantApi(
            this IServiceCollection serviceCollection,
            string audience,
            Dictionary<string, string> tenantProviders,
            List<string> scopes
        )
        {
            var authenticationBuilder = serviceCollection.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = ApiConstant.MultiTenantAuthenticationScheme;
                options.DefaultChallengeScheme = ApiConstant.MultiTenantAuthenticationScheme;
            });
            foreach (var (tenantHostname, providerAddress) in tenantProviders)
            {
                authenticationBuilder.AddJwtBearer(
                    tenantHostname,
                    options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateAudience = true,
                            ValidateIssuer = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidAudiences = new[] { audience },
                            ValidIssuer = providerAddress,
                        };
                        options.RequireHttpsMetadata = providerAddress.StartsWith("https://");
                        options.Authority = providerAddress;
                    }
                );
            }
            authenticationBuilder.AddPolicyScheme(
                ApiConstant.MultiTenantAuthenticationScheme,
                ApiConstant.MultiTenantAuthenticationScheme,
                options =>
                    options.ForwardDefaultSelector = (context) =>
                    {
                        // Use the host header to lookup the authentication scheme to actually authenticate with
                        return context.Request.Host.ToString();
                    }
            );
            serviceCollection.AddAuthorization(authorizationOptions =>
            {
                foreach (var scope in scopes)
                {
                    // Check the request is authenticated and has the required scope claim
                    authorizationOptions.AddApiPolicyScope(ApiConstant.MultiTenantAuthenticationScheme, scope);
                }
            });
            return serviceCollection;
        }
    }
}
