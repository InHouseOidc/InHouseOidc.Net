// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace InHouseOidc.Provider.Test
{
    [TestClass]
    public class DictionaryExtensionTest
    {
        [TestMethod]
        public void TryGetNonEmptyValue_FoundValue()
        {
            // Arrange
            var key = "Key";
            var value = "Value";
            var dictionary = new Dictionary<string, string> { { key, value } };
            // Act
            var result = dictionary.TryGetNonEmptyValue(key, out var resultValue);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(value, resultValue);
        }

        [TestMethod]
        public void TryGetNonEmptyValue_NotFoundValue()
        {
            // Arrange
            var key = "Key";
            var dictionary = new Dictionary<string, string>();
            // Act
            var result = dictionary.TryGetNonEmptyValue(key, out var resultValue);
            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(resultValue);
        }

        [TestMethod]
        public void TryGetNonEmptyValue_EmptyValue()
        {
            // Arrange
            var key = "Key";
            var value = string.Empty;
            var dictionary = new Dictionary<string, string> { { key, value } };
            // Act
            var result = dictionary.TryGetNonEmptyValue(key, out var resultValue);
            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(resultValue);
        }
    }
}
