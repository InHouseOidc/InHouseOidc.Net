// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.CredentialsClient.Type
{
    internal class ClientCredentialsToken
    {
        public ClientCredentialsToken(string accessToken, DateTimeOffset expiryUtc)
        {
            this.AccessToken = accessToken;
            this.ExpiryUtc = expiryUtc;
        }

        public string AccessToken { get; set; }
        public DateTimeOffset ExpiryUtc { get; set; }
    }
}
