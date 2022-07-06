// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace InHouseOidc.Common
{
    public static class EnumHelper
    {
        public static bool TryParseEnumMember<T>(string value, [MaybeNullWhen(returnValue: false)] out T enumValue)
            where T : Enum
        {
            var type = typeof(T);
            foreach (var field in type.GetFields())
            {
                if (Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) is EnumMemberAttribute attribute)
                {
                    if (value.Equals(attribute.Value))
                    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                        enumValue = (T)field.GetValue(null);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
                        return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
                    }
                }
                else if (value.Equals(field.Name))
                {
                    var fieldValue = field.GetValue(null);
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
                    enumValue = (T)field.GetValue(null);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8762 // Parameter must have a non-null value when exiting in some condition.
                    return true;
#pragma warning restore CS8762 // Parameter must have a non-null value when exiting in some condition.
                }
            }
            enumValue = default;
            return false;
        }

        public static T ParseEnumMember<T>(string value) where T : Enum
        {
            if (TryParseEnumMember<T>(value, out var enumValue))
            {
                return enumValue;
            }
            throw new ArgumentException("Invalid enum member value", nameof(value));
        }
    }
}
