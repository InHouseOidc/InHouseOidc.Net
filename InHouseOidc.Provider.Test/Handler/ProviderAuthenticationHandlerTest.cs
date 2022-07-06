// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Handler;
using InHouseOidc.Provider.Type;
using InHouseOidc.Test.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace InHouseOidc.Provider.Test.Handler
{
    [TestClass]
    public class ProviderAuthenticationHandlerTest
    {
        private readonly Mock<IOptionsMonitor<AuthenticationSchemeOptions>> mockIOptionsMonitor =
            new(MockBehavior.Strict);
        private readonly Mock<ISystemClock> mockSystemClock = new(MockBehavior.Strict);
        private readonly DateTimeOffset utcNow = new DateTimeOffset(
            2022,
            5,
            17,
            17,
            13,
            00,
            TimeSpan.Zero
        ).ToUniversalTime();
        private readonly TestLogger<ProviderAuthenticationHandler> logger = new();

        private Mock<ILoggerFactory> mockLoggerFactory = new(MockBehavior.Strict);
        private ProviderOptions providerOptions = new();

        [TestInitialize]
        public void Initialise()
        {
            this.mockLoggerFactory = new(MockBehavior.Strict);
            this.mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(this.logger);
            this.logger.Clear();
            this.mockIOptionsMonitor.Setup(m => m.Get(It.IsAny<string>())).Returns(new AuthenticationSchemeOptions());
            var mockIOptionsMonitor = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>(MockBehavior.Strict);
            this.providerOptions = new();
            this.mockSystemClock.Setup(m => m.UtcNow).Returns(this.utcNow);
        }

        [DataTestMethod]
        [DataRow("{null}")]
        [DataRow("")]
        [DataRow("/")]
        [DataRow("/ignored")]
        public async Task HandleRequestAsync_IgnoredPath(string? pathString)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = new DefaultHttpContext();
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationScheme,
                null,
                typeof(ProviderAuthenticationHandler)
            );
            var providerAuthenticationHandler = new ProviderAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.providerOptions,
                serviceProvider,
                UrlEncoder.Default,
                this.mockSystemClock.Object
            );
            await providerAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            context.Request.Path = pathString == "{null}" ? null : new PathString(pathString);
            // Act
            var result = await providerAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task HandleRequestAsync_AuthorizationCode()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var mockEndpointHandler = new Mock<IEndpointHandler<AuthorizationHandler>>(MockBehavior.Strict);
            mockEndpointHandler.Setup(m => m.HandleRequest(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            serviceCollection.AddScoped(
                typeof(IEndpointHandler<AuthorizationHandler>),
                (serviceProvider) => mockEndpointHandler.Object
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = new DefaultHttpContext();
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationScheme,
                null,
                typeof(ProviderAuthenticationHandler)
            );
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            var providerAuthenticationHandler = new ProviderAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.providerOptions,
                serviceProvider,
                UrlEncoder.Default,
                this.mockSystemClock.Object
            );
            await providerAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            context.Request.Path = new PathString(this.providerOptions.AuthorizationEndpointUri.OriginalString);
            // Act
            var result = await providerAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task HandleRequestAsync_CheckSession()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var mockEndpointHandler = new Mock<IEndpointHandler<CheckSessionHandler>>(MockBehavior.Strict);
            mockEndpointHandler.Setup(m => m.HandleRequest(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            serviceCollection.AddScoped(
                typeof(IEndpointHandler<CheckSessionHandler>),
                (serviceProvider) => mockEndpointHandler.Object
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = new DefaultHttpContext();
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationScheme,
                null,
                typeof(ProviderAuthenticationHandler)
            );
            this.providerOptions.CheckSessionEndpointEnabled = true;
            var providerAuthenticationHandler = new ProviderAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.providerOptions,
                serviceProvider,
                UrlEncoder.Default,
                this.mockSystemClock.Object
            );
            await providerAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            Assert.IsNotNull(this.providerOptions.CheckSessionEndpointUri);
            context.Request.Path = new PathString(this.providerOptions.CheckSessionEndpointUri.OriginalString);
            // Act
            var result = await providerAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task HandleRequestAsync_UserInfo()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var mockEndpointHandler = new Mock<IEndpointHandler<UserInfoHandler>>(MockBehavior.Strict);
            mockEndpointHandler.Setup(m => m.HandleRequest(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            serviceCollection.AddScoped(
                typeof(IEndpointHandler<UserInfoHandler>),
                (serviceProvider) => mockEndpointHandler.Object
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = new DefaultHttpContext();
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationScheme,
                null,
                typeof(ProviderAuthenticationHandler)
            );
            this.providerOptions.UserInfoEndpointEnabled = true;
            var providerAuthenticationHandler = new ProviderAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.providerOptions,
                serviceProvider,
                UrlEncoder.Default,
                this.mockSystemClock.Object
            );
            await providerAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            Assert.IsNotNull(this.providerOptions.UserInfoEndpointUri);
            context.Request.Path = new PathString(this.providerOptions.UserInfoEndpointUri.OriginalString);
            // Act
            var result = await providerAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task HandleRequestAsync_NullUriIgnored()
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = new DefaultHttpContext();
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationScheme,
                null,
                typeof(ProviderAuthenticationHandler)
            );
            this.providerOptions.GrantTypes.Add(GrantType.AuthorizationCode);
            var providerAuthenticationHandler = new ProviderAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.providerOptions,
                serviceProvider,
                UrlEncoder.Default,
                this.mockSystemClock.Object
            );
            await providerAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            context.Request.Path = "/connect/authorize";
            // Act
            var result = await providerAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.IsFalse(result);
        }

        [DataTestMethod]
        [DynamicData(nameof(HandleRequestAsync_Exception_Data))]
        public async Task HandleRequestAsync_Exception(
            System.Exception exception,
            int statusCode,
            string expectedBodyOrLocation,
            LogLevel expectedLogLevel,
            string expectedLogMessage,
            bool expectExceptionLogged
        )
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            var context = new DefaultHttpContext();
            context.Request.Path = "/connect/token";
            context.Response.Body = new MemoryStream();
            var mockEndpointHandler = new Mock<IEndpointHandler<TokenHandler>>(MockBehavior.Strict);
            mockEndpointHandler.Setup(m => m.HandleRequest(It.IsAny<HttpRequest>())).ThrowsAsync(exception);
            serviceCollection.AddScoped(
                typeof(IEndpointHandler<TokenHandler>),
                (serviceProvider) => mockEndpointHandler.Object
            );
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationScheme,
                null,
                typeof(ProviderAuthenticationHandler)
            );
            this.providerOptions.LogFailuresAsInformation = expectedLogLevel == LogLevel.Information;
            var providerAuthenticationHandler = new ProviderAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.providerOptions,
                serviceProvider,
                UrlEncoder.Default,
                this.mockSystemClock.Object
            );
            await providerAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            // Act
            var result = await providerAuthenticationHandler.HandleRequestAsync();
            // Assert
            Assert.AreEqual(statusCode, context.Response.StatusCode);
            if (statusCode == (int)HttpStatusCode.Redirect)
            {
                Assert.AreEqual(expectedBodyOrLocation, context.Response.Headers["location"].ToString());
            }
            else
            {
                Assert.AreEqual(ContentTypeConstant.ApplicationJson, context.Response.ContentType);
                var responseBody = TestHelper.ReadBodyAsString(context.Response);
                Assert.IsNotNull(responseBody);
                Assert.AreEqual(expectedBodyOrLocation, responseBody);
            }
            Assert.AreEqual(1, this.logger.LogItems.Count);
            this.logger.AssertLastItemContains(expectedLogLevel, expectedLogMessage);
            if (expectExceptionLogged)
            {
                Assert.AreEqual(exception, this.logger.LogItems.Last().Exception);
            }
        }

        public static IEnumerable<object[]> HandleRequestAsync_Exception_Data
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        new BadRequestException("test_error", "Message {value}", "value"),
                        (int)HttpStatusCode.BadRequest,
                        JsonSerializer.Serialize(new { error = "test_error" }, JsonHelper.JsonSerializerOptions),
                        LogLevel.Information,
                        "Message value",
                        false,
                    },
                    new object[]
                    {
                        new InternalErrorException("Message {value}", "value"),
                        (int)HttpStatusCode.InternalServerError,
                        ExceptionConstant.InternalError,
                        LogLevel.Error,
                        "Message value",
                        false,
                    },
                    new object[]
                    {
                        new RedirectErrorException(
                            RedirectErrorType.ServerError,
                            "http://localhost",
                            "Message {value}",
                            "value"
                        ),
                        (int)HttpStatusCode.Redirect,
                        "http://localhost?error=server_error",
                        LogLevel.Information,
                        "Message value",
                        false,
                    },
                    new object[]
                    {
                        new RedirectErrorException(
                            RedirectErrorType.ServerError,
                            "http://localhost",
                            "Message {value}",
                            "value"
                        ),
                        (int)HttpStatusCode.Redirect,
                        "http://localhost?error=server_error",
                        LogLevel.Error,
                        "Message value",
                        false,
                    },
                    new object[]
                    {
                        new System.Exception("unexpected"),
                        (int)HttpStatusCode.InternalServerError,
                        ExceptionConstant.InternalError,
                        LogLevel.Error,
                        "Unhandled exception",
                        true,
                    },
                    new object[]
                    {
                        new System.Exception("unexpected"),
                        (int)HttpStatusCode.InternalServerError,
                        ExceptionConstant.InternalError,
                        LogLevel.Information,
                        "Unhandled exception",
                        true,
                    },
                    new object[]
                    {
                        new InvalidOperationException("invalid_operation"),
                        (int)HttpStatusCode.InternalServerError,
                        ExceptionConstant.InternalError,
                        LogLevel.Information,
                        "Unhandled exception",
                        true,
                    },
                };
            }
        }

        [TestMethod]
        public async Task HandleAuthenticateAsync_NotImplemented()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var serviceCollection = new TestServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var authenticationScheme = new AuthenticationScheme(
                ProviderConstant.AuthenticationScheme,
                null,
                typeof(ProviderAuthenticationHandler)
            );
            var providerAuthenticationHandler = new ProviderAuthenticationHandler(
                this.mockIOptionsMonitor.Object,
                this.mockLoggerFactory.Object,
                this.providerOptions,
                serviceProvider,
                UrlEncoder.Default,
                this.mockSystemClock.Object
            );
            await providerAuthenticationHandler.InitializeAsync(authenticationScheme, context);
            // Act
            var exception = await Assert.ThrowsExceptionAsync<NotImplementedException>(
                async () => await providerAuthenticationHandler.AuthenticateAsync()
            );
            // Assert
            Assert.IsNotNull(exception);
        }
    }
}
