// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Type;

namespace InHouseOidc.Provider.Exception
{
    internal class RedirectErrorException(
        RedirectErrorType redirectErrorType,
        string uri,
        string logMessage,
        params object[]? args
    ) : LogMessageException(logMessage, args)
    {
        public RedirectErrorType RedirectErrorType { get; private set; } = redirectErrorType;
        public string? SessionState { get; set; }
        public string? State { get; set; }
        public string Uri { get; private set; } = uri;
    }
}
