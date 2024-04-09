// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Provider
{
    public enum GrantType
    {
        None = 0,

        [EnumMember(Value = TokenEndpointConstant.AuthorizationCode)]
        AuthorizationCode = 1,

        [EnumMember(Value = TokenEndpointConstant.ClientCredentials)]
        ClientCredentials = 2,

        [EnumMember(Value = TokenEndpointConstant.RefreshToken)]
        RefreshToken = 3,
    }
}
