// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Extension;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Common.Test.Extension
{
    [TestClass]
    public class ExceptionExtensionTest
    {
        private readonly string message = "Message";

        [TestMethod]
        public void ExceptionExtension_IsRetryableHttpException_NoInnerException()
        {
            // Arrange
            var exception = new HttpRequestException(this.message);
            // Act
            var result = ExceptionExtension.IsRetryableHttpException(exception);
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ExceptionExtension_IsRetryableHttpException_IoException()
        {
            // Arrange
            var exception = new HttpRequestException(this.message, new IOException("The response ended prematurely."));
            // Act
            var result1 = ExceptionExtension.IsRetryableHttpException(exception);
            // Assert
            Assert.IsTrue(result1);
        }

        [TestMethod]
        public void ExceptionExtension_IsRetryableHttpException_SocketException()
        {
            // Arrange 1
            var exception = new HttpRequestException(this.message, new System.Net.Sockets.SocketException(10054));
            // Act 1
            var result1 = ExceptionExtension.IsRetryableHttpException(exception);
            // Assert 1
            Assert.IsTrue(result1, "Assert 1");
            // Arrange 2
            exception = new HttpRequestException(this.message, new System.Net.Sockets.SocketException(10061));
            // Act 2
            var result2 = ExceptionExtension.IsRetryableHttpException(exception);
            // Assert 2
            Assert.IsTrue(result2, "Assert 2");
        }

        [TestMethod]
        public void ExceptionExtension_IsRetryableHttpException_WinHttpException()
        {
            // Arrange
            var innerMessage = "A connection with the server could not be established";
            var exception = new HttpRequestException(this.message, new WinHttpException(innerMessage));
            // Act
            var result = ExceptionExtension.IsRetryableHttpException(exception);
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ExceptionExtension_IsRetryableAggregateException_NoInnerExceptions()
        {
            // Arrange 1
            var exception1 = new AggregateException(new List<Exception>());
            // Act 1
            var result1 = ExceptionExtension.IsRetryableAggregateException(exception1);
            // Assert 1
            Assert.IsFalse(result1);
            // Arrange 2
            var exception2 = new AggregateException(new List<Exception> { new Exception(this.message) });
            // Act 2
            var result2 = ExceptionExtension.IsRetryableAggregateException(exception2);
            // Assert 2
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void ExceptionExtension_IsRetryableAggregateException_InnerExceptions()
        {
            // Arrange 1
            var innerExceptions1 = new List<Exception> { new System.Net.Sockets.SocketException(10061) };
            var exception1 = new AggregateException(innerExceptions1);
            // Act 1
            var result1 = ExceptionExtension.IsRetryableAggregateException(exception1);
            // Assert 1
            Assert.IsTrue(result1, "Assert 1");
            // Arrange 2
            var innerExceptions2 = new List<Exception> { new IOException("The response ended prematurely.") };
            var exception2 = new AggregateException(innerExceptions2);
            // Act 2
            var result2 = ExceptionExtension.IsRetryableAggregateException(exception2);
            // Assert 2
            Assert.IsTrue(result2, "Assert 2");
            // Arrange 3
            var innerMessage = "A connection with the server could not be established";
            var innerExceptions3 = new WinHttpException(innerMessage);
            var exception3 = new AggregateException(innerExceptions3);
            // Act 3
            var result3 = ExceptionExtension.IsRetryableAggregateException(exception3);
            // Assert 3
            Assert.IsTrue(result3, "Assert 3");
        }

        private class WinHttpException : Exception
        {
            public WinHttpException(string message)
                : base(message) { }
        }
    }
}
