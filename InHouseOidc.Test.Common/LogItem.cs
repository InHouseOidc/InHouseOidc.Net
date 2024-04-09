// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.Extensions.Logging;

namespace InHouseOidc.Test.Common
{
    public class LogItem(Exception? exception, LogLevel logLevel, string message)
    {
        public Exception? Exception { get; set; } = exception;
        public LogLevel LogLevel { get; set; } = logLevel;
        public string Message { get; set; } = message;
    }
}
