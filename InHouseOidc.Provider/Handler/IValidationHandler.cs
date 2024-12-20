﻿// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Type;

namespace InHouseOidc.Provider.Handler
{
    internal interface IValidationHandler
    {
        Task<(AuthorizationRequest?, RedirectError?)> ParseValidateAuthorizationRequest(
            Dictionary<string, string> parameters
        );
        Task<ClaimsPrincipal?> ValidateJsonWebToken(string? audience, string issuer, string jwt, bool validateLifetime);
    }
}
