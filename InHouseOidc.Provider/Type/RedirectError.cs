// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Constant;

namespace InHouseOidc.Provider.Type
{
    internal enum RedirectErrorType
    {
        None = 0,

        [EnumMember(Value = ProviderConstant.InvalidRequest)]
        InvalidRequest = 1,

        [EnumMember(Value = ProviderConstant.UnauthorizedClient)]
        UnauthorizedClient = 2,

        [EnumMember(Value = ProviderConstant.AccessDenied)]
        AccessDenied = 3,

        [EnumMember(Value = ProviderConstant.UnsupportedResponseType)]
        UnsupportedResponseType = 4,

        [EnumMember(Value = ProviderConstant.InvalidScope)]
        InvalidScope = 5,

        [EnumMember(Value = ProviderConstant.ServerError)]
        ServerError = 6,

        [EnumMember(Value = ProviderConstant.TemporarilyUnavailable)]
        TemporarilyUnavailable = 7,

        [EnumMember(Value = ProviderConstant.LoginRequired)]
        LoginRequired = 8,

        [EnumMember(Value = ProviderConstant.RequestNotSupported)]
        RequestNotSupported = 9,
    }

    internal class RedirectError(RedirectErrorType redirectErrorType, string logMessage, params object[]? args)
    {
        public object[]? Args { get; private set; } = args;
        public RedirectErrorType RedirectErrorType { get; private set; } = redirectErrorType;
        public string LogMessage { get; private set; } = logMessage;
        public string? RedirectUri { get; set; }
        public string? SessionState { get; set; }
        public string? State { get; set; }
    }
}
