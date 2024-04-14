// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Test.Common
{
    public class TestLogger<TType> : ILogger<TType>
    {
        public List<LogItem> LogItems { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            this.LogItems.Clear();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            var logItem = new LogItem(exception, logLevel, state?.ToString() ?? string.Empty);
            this.LogItems.Add(logItem);
        }

        public void AssertLastItemContains(LogLevel logLevel, string substring)
        {
            var lastItem = this.LogItems.LastOrDefault();
            Assert.IsNotNull(lastItem, "No items logged");
            Assert.AreEqual(logLevel, lastItem.LogLevel);
            StringAssert.Contains(lastItem.Message, substring);
        }
    }
}
