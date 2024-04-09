// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Common.Extension
{
    public static class StringExtension
    {
        public static string EnsureEndsWithSlash(this string value)
        {
            if (value.EndsWith('/'))
            {
                return value;
            }
            return $"{value}/";
        }
    }
}
