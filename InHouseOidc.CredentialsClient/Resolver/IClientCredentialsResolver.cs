// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.CredentialsClient.Resolver
{
    internal interface IClientCredentialsResolver
    {
        Task ClearClientToken(string clientName);
        Task<string?> GetClientToken(string clientName, CancellationToken cancellationToken);
    }
}
