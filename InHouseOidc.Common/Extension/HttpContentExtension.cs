// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Text.Json;

namespace InHouseOidc.Common.Extension
{
    public static class HttpContentExtension
    {
        public static async Task<TContentType?> ReadJsonAs<TContentType>(this HttpContent httpContent)
            where TContentType : class
        {
            var serialisedResponse = await httpContent.ReadAsStringAsync();
            if (string.IsNullOrEmpty(serialisedResponse))
            {
                return default;
            }
            return JsonSerializer.Deserialize<TContentType>(
                serialisedResponse,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );
        }
    }
}
