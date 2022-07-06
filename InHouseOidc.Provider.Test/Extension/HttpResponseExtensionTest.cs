// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Extension
{
    [TestClass]
    public class HttpResponseExtensionTest
    {
        private class Test
        {
            [JsonPropertyName("error")]
            public string? Error { get; set; }
        }

        [TestMethod]
        public async Task WriteStreamJsonContent()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var stringContent = JsonSerializer.Serialize(new Test { Error = "something-bad" });
            using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(stringContent));
            context.Response.Body = new MemoryStream();
            // Act
            await context.Response.WriteStreamJsonContent(memoryStream);
            // Assert
            Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
            Assert.AreEqual(stringContent, TestHelper.ReadBodyAsString(context.Response));
        }

        [TestMethod]
        public void WriteRedirect_ErrorCode()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var redirectUri = "http://localhost";
            var redirectErrorException = new RedirectErrorException(
                Type.RedirectErrorType.ServerError,
                redirectUri,
                "Log {value}",
                1
            );
            // Act
            context.Response.WriteRedirect(redirectErrorException);
            // Assert
            Assert.AreEqual(302, context.Response.StatusCode);
            Assert.AreEqual("http://localhost?error=server_error", context.Response.Headers["location"].ToString());
        }

        [TestMethod]
        public void WriteRedirect_BadErrorCode()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var redirectUri = "http://localhost";
            var redirectErrorException = new RedirectErrorException(
                Type.RedirectErrorType.None,
                redirectUri,
                "Log {value}",
                1
            );
            // Act
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => context.Response.WriteRedirect(redirectErrorException)
            );
            // Assert
            Assert.AreEqual("Unsupported ErrorCode value None", exception.Message);
        }

        [TestMethod]
        public void WriteRedirect_StateSessionState()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var redirectUri = "http://localhost";
            var redirectErrorException = new RedirectErrorException(
                Type.RedirectErrorType.ServerError,
                redirectUri,
                "Log {value}",
                1
            )
            {
                SessionState = "sessionstate",
                State = "state",
            };
            // Act
            context.Response.WriteRedirect(redirectErrorException);
            // Assert
            Assert.AreEqual(302, context.Response.StatusCode);
            Assert.AreEqual(
                "http://localhost?error=server_error&session_state=sessionstate&state=state",
                context.Response.Headers["location"].ToString()
            );
        }

        [TestMethod]
        public async Task WriteBadRequestContent()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var badRequestException = new BadRequestException("error_value", "Log {value}", 1);
            // Act
            await context.Response.WriteBadRequestContent(badRequestException);
            // Assert
            Assert.AreEqual(400, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
            Assert.AreEqual(
                JsonSerializer.Serialize(new Test { Error = "error_value" }, JsonHelper.JsonSerializerOptions),
                TestHelper.ReadBodyAsString(context.Response)
            );
        }

        [TestMethod]
        public async Task WriteInternalErrorContent()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            // Act
            await context.Response.WriteInternalErrorContent();
            // Assert
            Assert.AreEqual(500, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
            Assert.AreEqual(ExceptionConstant.InternalError, TestHelper.ReadBodyAsString(context.Response));
        }

        [TestMethod]
        public async Task WriteHtmlContent()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var htmlContent = "<html><body>Hello</body></html>";
            // Act
            await context.Response.WriteHtmlContent(htmlContent);
            // Assert
            Assert.AreEqual(200, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.TextHtml, context.Response.ContentType);
            Assert.AreEqual(htmlContent, TestHelper.ReadBodyAsString(context.Response));
        }

        [TestMethod]
        public void AppendSessionCookie()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var checkSessionCookieName = "check-session";
            var sessionId = "session-id";
            // Act
            context.Response.AppendSessionCookie(checkSessionCookieName, false, sessionId);
            // Assert
            var cookie = context.Response.Headers["cookies"];
            Assert.IsNotNull(cookie);
        }

        [TestMethod]
        public void DeleteSessionCookie()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var checkSessionCookieName = "check-session";
            var sessionId = "session-id";
            context.Response.AppendSessionCookie(checkSessionCookieName, false, sessionId);
            // Act
            context.Response.DeleteSessionCookie(checkSessionCookieName);
            // Assert
            var cookie = context.Response.Headers["cookies"];
            Assert.AreEqual(0, cookie.Count);
        }
    }
}
