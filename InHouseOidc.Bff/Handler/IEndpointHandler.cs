// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Handler
{
    internal interface IEndpointHandler
    {
        Task<bool> HandleRequest(HttpContext httpContext);
    }

    internal interface IEndpointHandler<THandler> : IEndpointHandler
        where THandler : class { }
}
