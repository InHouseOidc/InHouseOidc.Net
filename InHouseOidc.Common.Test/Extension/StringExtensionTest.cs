// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Common.Test.Extension
{
    [TestClass]
    public class StringExtensionTest
    {
        [DataTestMethod]
        [DataRow("http://localhost", "http://localhost/")]
        [DataRow("http://localhost/", "http://localhost/")]
        public void StringExtension_TestAll(string baseUri, string expectedUri)
        {
            // Act
            var result = baseUri.EnsureEndsWithSlash();
            // Assert
            Assert.AreEqual(expectedUri, result);
        }
    }
}
