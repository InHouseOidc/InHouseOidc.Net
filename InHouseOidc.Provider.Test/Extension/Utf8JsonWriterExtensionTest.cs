// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Provider.Test.Extension
{
    [TestClass]
    public class Utf8JsonWriterExtensionTest
    {
        private class TestSuccess
        {
            [JsonPropertyName("bool")]
            public bool? Boolean { get; set; }

            [JsonPropertyName("int")]
            public int? Integer { get; set; }

            [JsonPropertyName("string")]
            public string? Stringx { get; set; }
        }

        [TestMethod]
        public void Utf8JsonWriterExtension_Success()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            // Act
            utf8JsonWriter.WriteStartObject();
            utf8JsonWriter.WriteNameValue("bool", true);
            utf8JsonWriter.WriteNameValue("int", 1);
            utf8JsonWriter.WriteNameValue("string", "a");
            utf8JsonWriter.WriteEndObject();
            utf8JsonWriter.Flush();
            var result = Encoding.UTF8.GetString(memoryStream.ToArray());
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(
                JsonSerializer.Serialize(
                    new TestSuccess
                    {
                        Boolean = true,
                        Integer = 1,
                        Stringx = "a",
                    },
                    JsonHelper.JsonSerializerOptions
                ),
                result
            );
        }

        [TestMethod]
        public void Utf8JsonWriterExtension_UnsupportedType()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            utf8JsonWriter.WriteStartObject();
            // Act/Assert
            var exception = Assert.ThrowsException<ArgumentException>(
                () => utf8JsonWriter.WriteNameValue("datetime", DateTime.Now)
            );
            StringAssert.Contains(exception.Message, "Unsupported value type");
        }

        private class TestNamesValues
        {
            [JsonPropertyName("array")]
            public string[]? Array { get; set; }
        }

        [TestMethod]
        public void WriteNameValues()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            // Act
            utf8JsonWriter.WriteStartObject();
            utf8JsonWriter.WriteNameValues("array", ["one", "two"]);
            utf8JsonWriter.WriteEndObject();
            utf8JsonWriter.Flush();
            var result = Encoding.UTF8.GetString(memoryStream.ToArray());
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(
                JsonSerializer.Serialize(
                    new TestNamesValues { Array = ["one", "two"], },
                    JsonHelper.JsonSerializerOptions
                ),
                result
            );
        }

        [TestMethod]
        public void WriteNameUri_BadArguments()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            utf8JsonWriter.WriteStartObject();
            // Act/Assert
            var exception1 = Assert.ThrowsException<ArgumentNullException>(
                () => utf8JsonWriter.WriteNameUri("uri", null, null)
            );
            StringAssert.Contains(exception1.Message, "Value cannot be null");
            var exception2 = Assert.ThrowsException<ArgumentNullException>(
                () => utf8JsonWriter.WriteNameUri("uri", "http://localhost", null)
            );
            StringAssert.Contains(exception2.Message, "Value cannot be null");
        }

        private class TestNameUri
        {
            [JsonPropertyName("uri")]
            public string? Uri { get; set; }
        }

        [TestMethod]
        public void WriteNameUri_Success()
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            // Act
            utf8JsonWriter.WriteStartObject();
            utf8JsonWriter.WriteNameUri("uri", "http://localhost", new Uri("/path", UriKind.Relative));
            utf8JsonWriter.WriteEndObject();
            utf8JsonWriter.Flush();
            var result = Encoding.UTF8.GetString(memoryStream.ToArray());
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(
                JsonSerializer.Serialize(
                    new TestNameUri { Uri = "http://localhost/path" },
                    JsonHelper.JsonSerializerOptions
                ),
                result
            );
        }
    }
}
