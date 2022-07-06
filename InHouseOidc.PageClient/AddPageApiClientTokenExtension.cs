// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.PageClient.Handler;
using InHouseOidc.PageClient.Resolver;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.PageClient
{
    public static class AddPageApiClientTokenExtension
    {
        /// <summary>
        /// Adds OIDC client support for an MVC or Razor page based website to an HttpClientBuilder.<br />
        /// A message handler is added to any built HttpClient that automatically includes an access token header in all requests.
        /// </summary>
        /// <param name="httpClientBuilder">The HttpClientBuilder being configured during startup</param>
        /// <param name="clientName">The named HttpClient requiring access tokens.</param>
        /// <returns>ProviderBuilder.</returns>
        public static IHttpClientBuilder AddPageApiClientToken(
            this IHttpClientBuilder httpClientBuilder,
            string clientName
        )
        {
            return httpClientBuilder.AddHttpMessageHandler(serviceProvider =>
            {
                var pageAccessTokenResolver = serviceProvider.GetRequiredService<IPageAccessTokenResolver>();
                return new PageApiClientHandler(pageAccessTokenResolver, clientName);
            });
        }
    }
}
