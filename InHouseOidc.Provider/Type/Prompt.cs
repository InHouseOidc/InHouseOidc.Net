// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Provider.Type
{
    internal enum Prompt
    {
        NotSpecified = 0,

        [EnumMember(Value = DiscoveryConstant.PromptLogin)]
        Login = 1,

        [EnumMember(Value = DiscoveryConstant.PromptNone)]
        None = 2,
    }
}
