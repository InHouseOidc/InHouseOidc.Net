// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Extension;
using InHouseOidc.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static InHouseOidc.Common.Extension.HttpClientExtension;

namespace InHouseOidc.Common.Test.Extension
{
    [TestClass]
    public class HttpClientExtensionTest
    {
        private readonly TestMessageHandler testMessageHandler = new();
        private readonly TestLogger<HttpClientExtensionTest> logger = new();

        [TestInitialize]
        public void Initialise() { }

        [TestMethod]
        public async Task SendWithRetry_FormContentSuccess()
        {
            // Arrange
            var httpClient = new HttpClient(this.testMessageHandler);
            var uri = new Uri("http://localhost");
            var formContent = new FormUrlEncodedContent(new Dictionary<string, string> { { "abc", "123" } });
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
            // Act
            var response = await httpClient.SendWithRetry(
                HttpMethod.Get,
                uri,
                formContent,
                CancellationToken.None,
                1,
                50,
                this.logger
            );
            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(formContent, this.testMessageHandler.RequestMessage.Content);
            Assert.AreEqual(0, this.logger.LogItems.Count);
        }

        [TestMethod]
        public async Task SendWithRetry_JsonContentSuccess()
        {
            // Arrange
            var httpClient = new HttpClient(this.testMessageHandler);
            var uri = new Uri("http://localhost");
            var objectContent = new { Property = "value" };
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage { StatusCode = HttpStatusCode.OK };
            // Act
            var response = await httpClient.SendWithRetry(
                HttpMethod.Get,
                uri,
                objectContent,
                CancellationToken.None,
                1,
                50,
                this.logger
            );
            // Assert
            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.IsInstanceOfType(this.testMessageHandler.RequestMessage.Content, typeof(JsonContent));
            Assert.AreEqual(0, this.logger.LogItems.Count);
        }

        [DataTestMethod]
        [DataRow(HttpStatusCode.OK, false)]
        [DataRow(HttpStatusCode.RequestTimeout, true)]
        [DataRow(HttpStatusCode.BadGateway, true)]
        [DataRow(HttpStatusCode.ServiceUnavailable, true)]
        [DataRow(HttpStatusCode.GatewayTimeout, true)]
        public async Task SendWithRetry_RetryStatusCodes(HttpStatusCode httpStatusCode, bool throwsException)
        {
            // Arrange
            var httpClient = new HttpClient(this.testMessageHandler);
            var uri = new Uri("http://localhost");
            this.testMessageHandler.ResponseMessage = new HttpResponseMessage { StatusCode = httpStatusCode };
            if (throwsException)
            {
                // Act
                var exception = await Assert.ThrowsExceptionAsync<HttpClientRetryableException>(
                    async () =>
                        await httpClient.SendWithRetry(
                            HttpMethod.Get,
                            uri,
                            null,
                            CancellationToken.None,
                            1,
                            50,
                            this.logger
                        )
                );
                // Assert
                Assert.AreEqual(httpStatusCode, exception.StatusCode);
                Assert.IsTrue(exception.Message.Contains("Retryable status code"));
                this.logger.AssertLastItemContains(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    $"{nameof(this.SendWithRetry_RetryStatusCodes)} send retry"
                );
            }
            else
            {
                // Act
                var response = await httpClient.SendWithRetry(
                    HttpMethod.Get,
                    uri,
                    null,
                    CancellationToken.None,
                    1,
                    50,
                    this.logger
                );
                // Assert
                Assert.IsNotNull(response);
                Assert.IsTrue(response.IsSuccessStatusCode);
            }
        }

        [TestMethod]
        public async Task SendWithRetry_RetryHttpRequestExceptions()
        {
            // Arrange
            var httpClient = new HttpClient(this.testMessageHandler);
            var message = "exception occurred";
            var uri = new Uri("http://localhost");
            this.testMessageHandler.ThrowException = new HttpRequestException(
                message,
                new IOException("The response ended prematurely.")
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<HttpRequestException>(
                async () =>
                    await httpClient.SendWithRetry(
                        HttpMethod.Get,
                        uri,
                        null,
                        CancellationToken.None,
                        1,
                        50,
                        this.logger
                    )
            );
            // Assert
            Assert.IsTrue(exception.Message.Contains(message));
            this.logger.AssertLastItemContains(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                $"{nameof(this.SendWithRetry_RetryHttpRequestExceptions)} send retry"
            );
        }

        [TestMethod]
        public async Task SendWithRetry_RetryAggregateExceptions()
        {
            // Arrange
            var httpClient = new HttpClient(this.testMessageHandler);
            var message = "exception occurred";
            var uri = new Uri("http://localhost");
            this.testMessageHandler.ThrowException = new AggregateException(
                message,
                new List<Exception> { new IOException("The response ended prematurely.") }
            );
            // Act
            var exception = await Assert.ThrowsExceptionAsync<AggregateException>(
                async () =>
                    await httpClient.SendWithRetry(
                        HttpMethod.Get,
                        uri,
                        null,
                        CancellationToken.None,
                        1,
                        50,
                        this.logger
                    )
            );
            // Assert
            Assert.IsTrue(exception.Message.Contains(message));
            this.logger.AssertLastItemContains(
                Microsoft.Extensions.Logging.LogLevel.Warning,
                $"{nameof(this.SendWithRetry_RetryAggregateExceptions)} send retry"
            );
        }
    }
}
