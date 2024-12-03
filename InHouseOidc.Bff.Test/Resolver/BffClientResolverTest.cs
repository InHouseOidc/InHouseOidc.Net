// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Resolver;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;

namespace InHouseOidc.Bff.Test.Resolver
{
    [TestClass]
    public class BffClientResolverTest
    {
        private readonly BffClientOptions bffClientOptions =
            new()
            {
                ClientId = "bffclientid",
                ClientSecret = "topsecret",
                OidcProviderAddress = "http://localhost",
                Scope = "bffscope",
            };
        private readonly string hostname = "bfftest.com";

        [TestMethod]
        public void GetClient_MultiTenant_Failure()
        {
            // Arrange
            var clientOptions = new Type.ClientOptions();
            var bffClientResolver = new BffClientResolver(clientOptions);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(this.hostname);
            // Act
            var exception = Assert.ThrowsException<InvalidOperationException>(
                () => bffClientResolver.GetClient(httpContext)
            );
            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual($"Unable to resolve client options for hostname: {this.hostname}", exception.Message);
        }

        [TestMethod]
        public void GetClient_MultiTenant_Success()
        {
            // Arrange
            var clientOptions = new Type.ClientOptions();
            clientOptions.BffClientOptionsMultitenant.TryAdd(this.hostname, this.bffClientOptions);
            var bffClientResolver = new BffClientResolver(clientOptions);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(this.hostname);
            // Act
            var (resultClientOptions, resultScheme) = bffClientResolver.GetClient(httpContext);
            // Assert
            Assert.AreSame(this.bffClientOptions, resultClientOptions);
            Assert.AreEqual(this.hostname, resultScheme);
        }

        [TestMethod]
        public void GetClient_SingleTenant_Success()
        {
            // Arrange
            var clientOptions = new Type.ClientOptions { BffClientOptions = this.bffClientOptions, };
            var bffClientResolver = new BffClientResolver(clientOptions);
            var httpContext = new DefaultHttpContext();
            // Act
            var (resultClientOptions, resultScheme) = bffClientResolver.GetClient(httpContext);
            // Assert
            Assert.AreSame(clientOptions.BffClientOptions, resultClientOptions);
            Assert.AreEqual(OpenIdConnectDefaults.AuthenticationScheme, resultScheme);
        }
    }
}
