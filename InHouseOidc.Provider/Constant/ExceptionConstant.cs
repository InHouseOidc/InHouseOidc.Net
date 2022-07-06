// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Constant
{
    // TODO - use string interpolation constant with {Environment.NewLine} with C# 11
    internal static class ExceptionConstant
    {
        public const string Error = "error";
        public const string InternalError = "{\n  \"error\": \"server_error\"\n}";
        public const string UnhandledException = "Unhandled exception";
    }
}
