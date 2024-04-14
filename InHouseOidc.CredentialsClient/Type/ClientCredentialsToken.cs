// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.CredentialsClient.Type
{
    internal class ClientCredentialsToken(string accessToken, DateTimeOffset expiryUtc)
    {
        public string AccessToken { get; set; } = accessToken;
        public DateTimeOffset ExpiryUtc { get; set; } = expiryUtc;
    }
}
