// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Extension
{
    internal static class DictionaryExtension
    {
        public static bool TryGetNonEmptyValue(
            this Dictionary<string, string> dictionary,
            string key,
            [MaybeNullWhen(returnValue: false)] out string value
        )
        {
            if (dictionary.TryGetValue(key, out var foundValue))
            {
                if (string.IsNullOrEmpty(foundValue))
                {
                    value = default;
                    return false;
                }
                value = foundValue;
                return true;
            }
            value = default;
            return false;
        }
    }
}
