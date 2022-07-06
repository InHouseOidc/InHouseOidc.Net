// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Exception
{
    internal class LogMessageException : System.Exception
    {
        public object[] Args { get; private set; }
        public string LogMessage { get; private set; }

        public LogMessageException(string logMessage, params object[]? args)
        {
            this.Args = args ?? Array.Empty<object>();
            this.LogMessage = logMessage;
        }
    }
}
