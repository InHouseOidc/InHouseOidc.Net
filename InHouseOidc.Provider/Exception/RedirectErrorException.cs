// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Type;

namespace InHouseOidc.Provider.Exception
{
    internal class RedirectErrorException : LogMessageException
    {
        public RedirectErrorType RedirectErrorType { get; private set; }
        public string? SessionState { get; set; }
        public string? State { get; set; }
        public string Uri { get; private set; }

        public RedirectErrorException(
            RedirectErrorType redirectErrorType,
            string uri,
            string logMessage,
            params object[]? args
        )
            : base(logMessage, args)
        {
            this.RedirectErrorType = redirectErrorType;
            this.Uri = uri;
        }
    }
}
