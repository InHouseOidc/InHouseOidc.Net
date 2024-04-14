// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    public class AuthorizationRequestClaim(string type, string value)
    {
        public string Type { get; set; } = type;
        public string Value { get; set; } = value;
    }
}
