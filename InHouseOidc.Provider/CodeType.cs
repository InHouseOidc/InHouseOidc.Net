// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider
{
    public enum CodeType
    {
        None = 0,
        AuthorizationCode = 1,
        LogoutCode = 2,
        RefreshTokenCode = 3,
    }
}
