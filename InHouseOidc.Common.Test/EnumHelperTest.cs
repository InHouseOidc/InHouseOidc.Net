// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.Serialization;

namespace InHouseOidc.Common.Test
{
    [TestClass]
    public class EnumHelperTest
    {
        [TestMethod]
        public void TryParseEnumMember_MemberValueMatch()
        {
            // Act
            var result = EnumHelper.TryParseEnumMember<TestEnum>("value_1", out var enumValue);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(TestEnum.Value1, enumValue);
        }

        [TestMethod]
        public void TryParseEnumMember_NativeMatch()
        {
            // Act
            var result = EnumHelper.TryParseEnumMember<TestEnum>("Value2", out var enumValue);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(TestEnum.Value2, enumValue);
        }

        [TestMethod]
        public void TryParseEnumMember_NoMatch()
        {
            // Act
            var result = EnumHelper.TryParseEnumMember<TestEnum>("Value3", out var enumValue);
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(TestEnum.None, enumValue);
        }

        [TestMethod]
        public void TryParseEnumMember_NullMatch()
        {
            // Act
            var result = EnumHelper.TryParseEnumMember<TestEnum>("Value4", out var enumValue);
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(TestEnum.None, enumValue);
        }

        [TestMethod]
        public void ParseEnumMember_Match()
        {
            // Act
            var result = EnumHelper.ParseEnumMember<TestEnum>("value_1");
            // Assert
            Assert.AreEqual(TestEnum.Value1, result);
        }

        [TestMethod]
        public void ParseEnumMember_NoMatch()
        {
            // Act
            var exception = Assert.ThrowsException<ArgumentException>(
                () => EnumHelper.ParseEnumMember<TestEnum>("value_x")
            );
            // Assert
            StringAssert.Contains(exception.Message, "Invalid enum member value");
        }

        private enum TestEnum
        {
            None = 0,

            [EnumMember(Value = "value_1")]
            Value1 = 1,
            Value2 = 2,

            [EnumMember(Value = null)]
            Value3 = 3,
        }
    }
}
