// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Bff.Resolver;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.Bff
{
    public static class AddBffApiClientTokenExtension
    {
        /// <summary>
        /// Adds OIDC client support for a BFF API to an HttpClientBuilder.<br />
        /// A message handler is added to any built HttpClient that automatically includes an access token header in all requests.
        /// </summary>
        /// <param name="httpClientBuilder">The HttpClientBuilder being configured during startup.</param>
        /// <param name="clientName">The named HttpClient requiring access tokens.</param>
        /// <returns>ProviderBuilder.</returns>
        public static IHttpClientBuilder AddBffApiClientToken(
            this IHttpClientBuilder httpClientBuilder,
            string clientName
        )
        {
            return httpClientBuilder.AddHttpMessageHandler(serviceProvider =>
            {
                var bffAccessTokenResolver = serviceProvider.GetRequiredService<IBffAccessTokenResolver>();
                return new BffApiClientHandler(bffAccessTokenResolver, clientName);
            });
        }
    }
}
