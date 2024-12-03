// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Resolver;
using InHouseOidc.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InHouseOidc.Bff
{
    public static class BffBuilderExtension
    {
        /// <summary>
        /// Adds an OIDC client that supports BFF authentication using the authorization code flow.<br />
        /// Further configuration of one or more clients is expected using: <br />
        /// .AddClient(..).
        /// </summary>
        /// <param name="serviceCollection">The ServiceCollection being configured during startup.</param>
        /// <returns>BffClientBuilder.</returns>
        public static BffBuilder AddOidcBff(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton<IBffClientResolver, BffClientResolver>();
            serviceCollection.TryAddSingleton<IUtcNow, UtcNow>();
            serviceCollection.AddHttpContextAccessor();
            return new BffBuilder(serviceCollection);
        }
    }
}
