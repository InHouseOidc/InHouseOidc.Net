// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Exception
{
    internal class BadRequestException(string error, string logMessage, params object[]? args)
        : LogMessageException(logMessage, args)
    {
        public string Error { get; private set; } = error;
    }
}
