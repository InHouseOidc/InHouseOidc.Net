// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common
{
    public static class JsonHelper
    {
        public static readonly JsonWriterOptions JsonWriterOptions =
            new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, Indented = true };

        public static readonly JsonSerializerOptions JsonSerializerOptions =
            new()
            {
                Converters = { new JsonStringEnumConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true,
            };
    }
}
