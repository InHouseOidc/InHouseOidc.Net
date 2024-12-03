// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Resolver;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Test.Resolver
{
    [TestClass]
    public class QueryParamResolverTest
    {
        private static readonly string DefaultValue = "default";
        private static readonly string Name = "name";
        private static readonly string Value = "value";

        [TestMethod]
        public void GetValue_NotHttpGet()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            // Act
            var result = QueryParamResolver.GetValue(context.Request, DefaultValue, Name);
            // Assert
            Assert.AreEqual(DefaultValue, result);
        }

        [TestMethod]
        public void GetValue_NotFound()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.QueryString = new QueryString("?not=found");
            // Act
            var result = QueryParamResolver.GetValue(context.Request, DefaultValue, Name);
            // Assert
            Assert.AreEqual(DefaultValue, result);
        }

        [TestMethod]
        public void GetValue_Found()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Method = "GET";
            context.Request.QueryString = new QueryString($"?{Name}={Value}");
            // Act
            var result = QueryParamResolver.GetValue(context.Request, DefaultValue, Name);
            // Assert
            Assert.AreEqual(Value, result);
        }
    }
}
