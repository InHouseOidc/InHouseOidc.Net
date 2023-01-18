// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.CredentialsClient.Handler;
using InHouseOidc.CredentialsClient.Resolver;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.CredentialsClient
{
    public static class AddClientCredentialsTokenExtension
    {
        /// <summary>
        /// Adds OIDC client support for an client credentials to an HttpClientBuilder.<br />
        /// A message handler is added to any built HttpClient that automatically includes an Authorization header in all requests.
        /// </summary>
        /// <param name="httpClientBuilder">The HttpClientBuilder being configured during startup.</param>
        /// <returns>ProviderBuilder.</returns>
        public static IHttpClientBuilder AddClientCredentialsToken(this IHttpClientBuilder httpClientBuilder)
        {
            return httpClientBuilder.AddHttpMessageHandler(serviceProvider =>
            {
                var credentialsAccessTokenResolver = serviceProvider.GetRequiredService<IClientCredentialsResolver>();
                return new ClientCredentialsHandler(credentialsAccessTokenResolver, httpClientBuilder.Name);
            });
        }
    }
}
