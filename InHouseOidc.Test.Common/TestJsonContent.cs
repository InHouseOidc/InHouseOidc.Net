// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace InHouseOidc.Test.Common
{
    public class TestJsonContent : StringContent
    {
        public TestJsonContent(object? obj)
            : base(
                JsonSerializer.Serialize(
                    obj,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                ),
                Encoding.UTF8,
                "application/json"
            ) { }
    }
}
