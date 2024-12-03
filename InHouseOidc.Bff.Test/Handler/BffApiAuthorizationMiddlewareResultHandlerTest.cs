// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Bff.Handler;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InHouseOidc.Bff.Test.Handler
{
    [TestClass]
    public class BffApiAuthorizationMiddlewareResultHandlerTest
    {
        private AuthorizationOptions authorizationOptions = new();
        private BffApiAuthorizationMiddlewareResultHandler handler = new();
        private TestServiceCollection serviceCollection = [];
        private ServiceProvider serviceProvider = new TestServiceCollection().BuildServiceProvider();
        private AuthorizationPolicy? policy;
        private PolicyAuthorizationResult authorizeResult = PolicyAuthorizationResult.Success();

        [TestInitialize]
        public void Initialise()
        {
            this.handler = new();
            this.serviceCollection = [];
            this.serviceCollection.AddOidcBff().Build();
            this.serviceProvider = this.serviceCollection.BuildServiceProvider();
            this.authorizationOptions = this.serviceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
            this.policy = this.authorizationOptions.GetPolicy(BffConstant.BffApiPolicy);
            this.authorizeResult = PolicyAuthorizationResult.Success();
        }

        [TestMethod]
        public async Task HandleAsync_Success()
        {
            // Arrange
            var claimsIdentity = new ClaimsIdentity([], BffConstant.AuthenticationSchemeBff);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            var context = new DefaultHttpContext { User = claimsPrincipal };
            Assert.IsNotNull(this.policy);
            // Act
            await this.handler.HandleAsync(Next, context, this.policy, this.authorizeResult);
            // Assert
            Assert.AreEqual(200, context.Response.StatusCode);
        }

        [TestMethod]
        public async Task HandleAsync_NoIdentityFailure()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal([]);
            var context = new DefaultHttpContext { User = claimsPrincipal };
            Assert.IsNotNull(this.policy);
            // Act
            await this.handler.HandleAsync(Next, context, this.policy, this.authorizeResult);
            // Assert
            Assert.AreEqual(401, context.Response.StatusCode);
        }

        private static Task Next(HttpContext context)
        {
            return Task.CompletedTask;
        }
    }
}
