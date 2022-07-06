// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Runtime.Serialization;

namespace InHouseOidc.Provider.Extension
{
    internal static class EnumExtension
    {
        public static List<string> ToStringList<T>(this List<T> values) where T : Enum
        {
            var type = typeof(T);
            var results = new List<string>();
            foreach (var value in values)
            {
                var memberInfo = type.GetMember(value.ToString());
                if (
                    memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault()
                        is EnumMemberAttribute attribute
                    && attribute.Value != null
                )
                {
                    results.Add(attribute.Value);
                }
            }
            return results;
        }

        public static string? GetEnumMember<T>(this T value) where T : Enum
        {
            var type = typeof(T);
            var memberInfo = type.GetMember(value.ToString());
            if (
                memberInfo[0].GetCustomAttributes(typeof(EnumMemberAttribute), false).FirstOrDefault()
                    is EnumMemberAttribute attribute
                && attribute.Value != null
            )
            {
                return attribute.Value;
            }
            return null;
        }
    }
}
