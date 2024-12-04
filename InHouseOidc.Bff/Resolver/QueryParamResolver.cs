// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Resolver
{
    internal static class QueryParamResolver
    {
        public static string GetValue(HttpRequest httpRequest, string defaultValue, string name)
        {
            var queryCollection = httpRequest.Query;
            if (queryCollection.Count == 0)
            {
                return defaultValue;
            }
            if (!queryCollection.TryGetValue(name, out var value))
            {
                return defaultValue;
            }
            return value.ToString();
        }
    }
}
