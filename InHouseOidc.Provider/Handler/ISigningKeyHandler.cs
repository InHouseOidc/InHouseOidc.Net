// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Type;

namespace InHouseOidc.Provider.Handler
{
    internal interface ISigningKeyHandler
    {
        Task<List<SigningKey>> Resolve();
    }
}
