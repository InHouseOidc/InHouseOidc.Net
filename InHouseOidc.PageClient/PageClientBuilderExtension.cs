// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InHouseOidc.PageClient
{
    public static class PageClientBuilderExtension
    {
        /// <summary>
        /// Adds an OIDC client that supports Razor (or MVC) page authentication using the authorization code flow.<br />
        /// Further configuration of one or more clients is expected using: <br />
        /// .AddClient(..).
        /// </summary>
        /// <param name="serviceCollection">The ServiceCollection being configured during startup.</param>
        /// <returns>PageClientBuilder.</returns>
        public static PageClientBuilder AddOidcPageClient(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.TryAddSingleton<IUtcNow, UtcNow>();
            serviceCollection.AddHttpContextAccessor();
            return new PageClientBuilder(serviceCollection);
        }
    }
}
