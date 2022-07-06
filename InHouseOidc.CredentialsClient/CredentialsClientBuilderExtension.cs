// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InHouseOidc.CredentialsClient
{
    public static class CredentialsClientBuilderExtension
    {
        /// <summary>
        /// Adds an OIDC client that supports API authentication using the client credentials flow.<br />
        /// Further configuration of one or more clients is expected using: <br />
        /// .AddClient(..).
        /// </summary>
        /// <param name="serviceCollection">The ServiceCollection being configured during startup.</param>
        /// <returns>CredentialsClientBuilder.</returns>
        public static CredentialsClientBuilder AddOidcCredentialsClient(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.TryAddSingleton<IUtcNow, UtcNow>();
            return new CredentialsClientBuilder(serviceCollection);
        }
    }
}
