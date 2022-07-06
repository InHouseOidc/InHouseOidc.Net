// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.Extensions.Logging;
using System;

namespace InHouseOidc.Test.Common
{
    public class LogItem
    {
        public LogItem(Exception? exception, LogLevel logLevel, string message)
        {
            this.Exception = exception;
            this.LogLevel = logLevel;
            this.Message = message;
        }

        public Exception? Exception { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
    }
}
