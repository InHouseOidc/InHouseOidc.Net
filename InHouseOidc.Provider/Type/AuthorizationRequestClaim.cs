// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    public class AuthorizationRequestClaim
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public AuthorizationRequestClaim(string type, string value)
        {
            this.Type = type;
            this.Value = value;
        }
    }
}
