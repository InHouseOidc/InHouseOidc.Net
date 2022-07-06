// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    internal class ClientValidation
    {
        public OidcClient OidcClient { get; set; } = new();
        public string ClientId { get; set; } = string.Empty;
        public object[]? ErrorArgs { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
