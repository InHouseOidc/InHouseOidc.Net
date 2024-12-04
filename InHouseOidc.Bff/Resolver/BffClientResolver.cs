// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Type;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Resolver
{
    internal class BffClientResolver(ClientOptions clientOptions) : IBffClientResolver
    {
        private readonly ClientOptions clientOptions = clientOptions;

        public (BffClientOptions, string) GetClient(HttpContext httpContext)
        {
            if (this.clientOptions.BffClientOptions != null)
            {
                return (this.clientOptions.BffClientOptions, OpenIdConnectDefaults.AuthenticationScheme);
            }
            var hostname = httpContext.Request.Host.ToString();
            if (!this.clientOptions.BffClientOptionsMultitenant.TryGetValue(hostname, out var bffClientOptions))
            {
                throw new InvalidOperationException($"Unable to resolve client options for hostname: {hostname}");
            }
            return (bffClientOptions, hostname);
        }
    }
}
