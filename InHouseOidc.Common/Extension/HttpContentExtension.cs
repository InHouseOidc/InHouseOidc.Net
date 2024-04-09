// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common.Extension
{
    public static class HttpContentExtension
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static async Task<TContentType?> ReadJsonAs<TContentType>(this HttpContent httpContent)
            where TContentType : class
        {
            var serialisedResponse = await httpContent.ReadAsStringAsync();
            if (string.IsNullOrEmpty(serialisedResponse))
            {
                return default;
            }
            return JsonSerializer.Deserialize<TContentType>(serialisedResponse, JsonSerializerOptions);
        }
    }
}
