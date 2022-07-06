// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    internal class ApiAuthenticationOptions
    {
        public string? Audience { get; set; }
        public List<string>? Scopes { get; set; }
    }
}
