// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Discovery
{
    public interface IDiscoveryResolver
    {
        Task<Discovery?> GetDiscovery(
            DiscoveryOptions discoveryOptions,
            string oidcProviderAddress,
            CancellationToken cancellationToken
        );
    }
}
