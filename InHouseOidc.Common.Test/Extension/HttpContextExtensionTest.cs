// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Common.Test.Extension
{
    [TestClass]
    public class HttpContextExtensionTest
    {
        [TestMethod]
        public async Task ReadJsonAs_NullResult()
        {
            // Arrange
            var httpContent = new StringContent(string.Empty);
            // Act
            var result = await httpContent.ReadJsonAs<TestClass>();
            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task ReadJsonAs_NotNullResult()
        {
            // Arrange
            var httpContent = new StringContent("{ \"value\": 1 }");
            // Act
            var result = await httpContent.ReadJsonAs<TestClass>();
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Value);
        }

        private class TestClass
        {
            public int? Value { get; set; }
        }
    }
}
