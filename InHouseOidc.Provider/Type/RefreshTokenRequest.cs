// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    public class RefreshTokenRequest
    {
        public string ClientId { get; set; }
        public string Scope { get; set; }
        public DateTimeOffset SessionExpiryUtc { get; set; }

        public RefreshTokenRequest(string clientId, string scope, DateTimeOffset sessionExpiryUtc)
        {
            this.ClientId = clientId;
            this.Scope = scope;
            this.SessionExpiryUtc = sessionExpiryUtc;
        }
    }
}
