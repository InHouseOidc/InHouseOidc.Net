// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Provider.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Provider.Test.Extension
{
    [TestClass]
    public class EnumExtensionTest
    {
        [TestMethod]
        public void ToStringList()
        {
            // Arrange
            var enums = new List<TestEnum> { TestEnum.Value1, TestEnum.Value2, TestEnum.Value3 };
            // Act
            var results = enums.ToStringList();
            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual("value_1", results.First());
            Assert.AreEqual("value_2", results.Last());
        }

        [TestMethod]
        public void GetEnumMember()
        {
            // Act 1
            var result1 = TestEnum.Value1.GetEnumMember();
            // Assert 1
            Assert.IsNotNull(result1);
            Assert.AreEqual("value_1", result1);
            // Act 2
            var result2 = TestEnum.Value3.GetEnumMember();
            // Assert 2
            Assert.IsNull(result2);
        }

        private enum TestEnum
        {
            None = 0,

            [EnumMember(Value = "value_1")]
            Value1 = 1,

            [EnumMember(Value = "value_2")]
            Value2 = 2,

            // No enum member here, value should be ignored
            Value3 = 3,
        }
    }
}
