// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Bff.Resolver
{
    internal interface IBffAccessTokenResolver
    {
        Task<string?> GetClientToken(string clientName, CancellationToken cancellationToken);
    }
}
