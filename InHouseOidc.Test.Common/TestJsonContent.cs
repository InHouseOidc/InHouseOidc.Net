// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Test.Common
{
    public class TestJsonContent(object? obj)
        : StringContent(JsonSerializer.Serialize(obj, JsonSerializerOptions), Encoding.UTF8, "application/json")
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }
}
