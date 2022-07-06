// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Exception
{
    internal class InternalErrorException : LogMessageException
    {
        public InternalErrorException(string logMessage, params object[]? args) : base(logMessage, args) { }
    }
}
