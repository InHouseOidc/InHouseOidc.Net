// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class DiscoveryHandlerTest
    {
        private readonly string host = "localhost";
        private readonly string urlScheme = "https";

        [TestMethod]
        public async Task HandleRequest_InvalidMethod()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "DELETE";
            context.Request.Scheme = this.urlScheme;
            var providerOptions = new ProviderOptions();
            var discoveryHandler = new DiscoveryHandler(providerOptions);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<BadRequestException>(
                async () => await discoveryHandler.HandleRequest(context.Request)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual("HttpMethod not supported: {method}", exception.LogMessage);
        }

        [TestMethod]
        public async Task HandleRequest_Compulsory()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Response.Body = new MemoryStream();
            var providerOptions = new ProviderOptions();
            var discoveryHandler = new DiscoveryHandler(providerOptions);
            // Act
            var result = await discoveryHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            var responseBody = TestHelper.ReadBodyAsString(context.Response);
            var discoveryResponse = JsonSerializer.Deserialize<Common.Type.DiscoveryResponse>(responseBody);
            Assert.IsNotNull(discoveryResponse);
            Assert.AreEqual($"{this.urlScheme}://{this.host}", discoveryResponse.Issuer);
            Assert.AreEqual($"{this.urlScheme}://{this.host}/connect/endsession", discoveryResponse.EndSessionEndpoint);
            Assert.AreEqual($"{this.urlScheme}://{this.host}/connect/token", discoveryResponse.TokenEndpoint);
            var expectedAuthMethods = new string[]
            {
                DiscoveryConstant.ClientSecretPost,
                DiscoveryConstant.ClientSecretBasic,
            };
            CollectionAssert.AreEqual(expectedAuthMethods, discoveryResponse.TokenEndpointAuthMethodsSupported);
        }

        [TestMethod]
        public async Task HandleRequest_Optional()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers.Host = this.host;
            context.Request.Method = "GET";
            context.Request.Scheme = this.urlScheme;
            context.Response.Body = new MemoryStream();
            var providerOptions = new ProviderOptions
            {
                CheckSessionEndpointEnabled = true,
                UserInfoEndpointEnabled = true,
            };
            providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            var discoveryHandler = new DiscoveryHandler(providerOptions);
            // Act
            var result = await discoveryHandler.HandleRequest(context.Request);
            // Assert
            Assert.IsTrue(result);
            var responseBody = TestHelper.ReadBodyAsString(context.Response);
            var discoveryResponse = JsonSerializer.Deserialize<Common.Type.DiscoveryResponse>(responseBody);
            Assert.IsNotNull(discoveryResponse);
            CollectionAssert.AreEqual(
                new string[] { TokenEndpointConstant.AuthorizationCode },
                discoveryResponse.GrantTypesSupported
            );
        }
    }
}
