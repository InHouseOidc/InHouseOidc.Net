// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Resolver
{
    internal interface IBffClientResolver
    {
        (BffClientOptions bffClientOptions, string scheme) GetClient(HttpContext httpContext);
    }
}
