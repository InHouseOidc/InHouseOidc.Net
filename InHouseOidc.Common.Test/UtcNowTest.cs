// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace InHouseOidc.Common.Test
{
    [TestClass]
    public class UtcNowTest
    {
        [TestMethod]
        public void UtcNow()
        {
            // Arrange
            var utcNow = (IUtcNow)new UtcNow();
            var before = DateTimeOffset.UtcNow;
            // Act
            var nowUtc = utcNow.UtcNow;
            // Assert
            var differenceSeconds = Math.Abs((nowUtc - before).TotalSeconds);
            Assert.IsTrue(differenceSeconds >= 0, "Expected difference seconds to be greater than or equal to zero");
        }
    }
}
