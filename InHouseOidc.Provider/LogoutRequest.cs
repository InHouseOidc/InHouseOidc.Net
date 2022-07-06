// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    public class LogoutRequest
    {
        public string? IdTokenHint { get; init; }
        public string? PostLogoutRedirectUri { get; init; }
        public string? State { get; init; }
        public string? Subject { get; init; }
    }
}
