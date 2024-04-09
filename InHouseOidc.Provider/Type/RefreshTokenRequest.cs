// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    public class RefreshTokenRequest(string clientId, string scope, DateTimeOffset sessionExpiryUtc)
    {
        public string ClientId { get; set; } = clientId;
        public string Scope { get; set; } = scope;
        public DateTimeOffset SessionExpiryUtc { get; set; } = sessionExpiryUtc;
    }
}
