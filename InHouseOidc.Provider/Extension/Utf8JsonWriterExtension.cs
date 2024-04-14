// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Extension
{
    internal static class Utf8JsonWriterExtension
    {
        public static void WriteNameValue(this Utf8JsonWriter utf8JsonWriter, string name, object value)
        {
            utf8JsonWriter.WritePropertyName(name);
            switch (value)
            {
                case bool boolValue:
                    utf8JsonWriter.WriteBooleanValue(boolValue);
                    break;
                case int intValue:
                    utf8JsonWriter.WriteNumberValue(intValue);
                    break;
                case string stringValue:
                    utf8JsonWriter.WriteStringValue(stringValue);
                    break;
                default:
                    throw new ArgumentException("Unsupported value type");
            }
        }

        public static void WriteNameValues(this Utf8JsonWriter utf8JsonWriter, string name, IEnumerable<string> values)
        {
            utf8JsonWriter.WritePropertyName(name);
            utf8JsonWriter.WriteStartArray();
            foreach (string value in values)
            {
                utf8JsonWriter.WriteStringValue(value);
            }
            utf8JsonWriter.WriteEndArray();
        }

        public static void WriteNameUri(this Utf8JsonWriter utf8JsonWriter, string name, string? issuer, Uri? valueUri)
        {
            ArgumentNullException.ThrowIfNull(issuer);
            ArgumentNullException.ThrowIfNull(valueUri);
            utf8JsonWriter.WritePropertyName(name);
            utf8JsonWriter.WriteStringValue(new Uri(new Uri(issuer), valueUri).AbsoluteUri.ToString());
        }
    }
}
