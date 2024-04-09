// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Common.Test
{
    [TestClass]
    public class JsonHelperTest
    {
        [TestMethod]
        public void JsonWriterOptions()
        {
            // Act
            var result = JsonHelper.JsonWriterOptions;
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, result.Encoder);
            Assert.IsTrue(result.Indented);
        }

        [TestMethod]
        public void JsonSerializerOptions()
        {
            // Act
            var result = JsonHelper.JsonSerializerOptions;
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, result.Encoder);
            Assert.IsTrue(result.WriteIndented);
        }
    }
}
