// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class CheckSessionHandlerTest
    {
        [TestMethod]
        public async Task HandleRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var providerOptions = new ProviderOptions { CheckSessionCookieName = "testcookienameloaded" };
            var checkSessionHandler = new CheckSessionHandler(providerOptions);
            // Act
            var result = await checkSessionHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(200, context.Response.StatusCode);
            Assert.AreEqual(ContentTypeConstant.TextHtml, context.Response.ContentType);
            using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = reader.ReadToEnd();
            Assert.IsNotNull(body);
            StringAssert.Contains(body, "<!DOCTYPE html>");
            StringAssert.Contains(body, providerOptions.CheckSessionCookieName);
            StringAssert.Contains(body, "window.addEventListener");
        }
    }
}
