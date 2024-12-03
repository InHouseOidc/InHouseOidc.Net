// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider.Handler;
using Microsoft.Extensions.DependencyInjection;

namespace InHouseOidc.Provider
{
    public static class AddOidcProviderExtension
    {
        /// <summary>
        /// Adds an OIDC Provider that uses cookies for session management.<br />
        /// Further configuration is expected for signing certificates (.SetSigningCertificates(...)),<br />
        /// and for supported flows (e.g. .EnableClientCredentialsFlow()).
        /// </summary>
        /// <param name="serviceCollection">The ServiceCollection being configured during startup.</param>
        /// <returns>ProviderBuilder.</returns>
        public static ProviderBuilder AddOidcProvider(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddOptions();
            serviceCollection.AddLogging();
            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddSingleton(typeof(IAsyncLock<>), typeof(AsyncLock<>));
            serviceCollection.AddScoped<IEndpointHandler<DiscoveryHandler>, DiscoveryHandler>();
            serviceCollection.AddScoped<IEndpointHandler<JsonWebKeySetHandler>, JsonWebKeySetHandler>();
            serviceCollection.AddSingleton<IJsonWebTokenHandler, JsonWebTokenHandler>();
            serviceCollection.AddSingleton<ISigningKeyHandler, SigningKeyHandler>();
            serviceCollection.AddScoped<IEndpointHandler<TokenHandler>, TokenHandler>();
            serviceCollection.AddSingleton<IValidationHandler, ValidationHandler>();
            serviceCollection.AddSingleton<IUtcNow, UtcNow>();
            return new ProviderBuilder(serviceCollection);
        }
    }
}
