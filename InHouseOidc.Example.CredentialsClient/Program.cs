// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;
using InHouseOidc.CredentialsClient;
using InHouseOidc.Example.CredentialsClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(
    configure =>
        configure.AddSimpleConsole(
            simpleConsoleFormatterOptions => simpleConsoleFormatterOptions.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] "
        )
);

// Setup the OIDC client
const string clientNameStartup = "exampleapistartup";
const string clientNameStored = "exampleapistored";
const string clientNameDynamic = "exampleapidynamic";
const string providerAddress = "http://localhost:5100";
const string clientId = "clientcredentialsexample";
const string clientSecret = "topsecret";
const string scope = "exampleapiscope";
serviceCollection
    .AddSingleton<ICredentialsStore, CredentialsStore>()
    .AddHttpClient(clientNameStored)
    .AddClientCredentialsToken();
serviceCollection.AddHttpClient(clientNameDynamic);
serviceCollection
    .AddOidcCredentialsClient()
    .AddClient(
        clientNameStartup,
        new CredentialsClientOptions
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            OidcProviderAddress = providerAddress,
            Scope = scope,
        }
    )
    .Build();

var serviceProvider = serviceCollection.BuildServiceProvider();

const string apiAddress = "http://localhost:5102";
Console.WriteLine(
    "Get Discovery and post token should only occur on the first call, or when the access token or discovery has expired."
);
while (true)
{
    // Loop calling the secure endpoint every time the enter key is pressed
    Console.WriteLine("Press [enter] to make an API call, or [ctrl-c] to exit.");
    Console.ReadLine();
    var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("main");
    try
    {
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        // Startup configured client
        var httpClientStartup = httpClientFactory.CreateClient(clientNameStartup);
        var responseStartup = await httpClientStartup.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
        responseStartup.EnsureSuccessStatusCode();
        var responseContentStartup = await responseStartup.Content.ReadAsStringAsync();
        logger.LogInformation("[Startup HttpClient] /secure response: {responseContent}", responseContentStartup);
        // Stored (via IClientStore) configured client
        var httpClientStored = httpClientFactory.CreateClient(clientNameStored);
        var responseStored = await httpClientStored.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
        responseStored.EnsureSuccessStatusCode();
        var responseContentStored = await responseStored.Content.ReadAsStringAsync();
        logger.LogInformation("[Stored HttpClient] /secure response: {responseContent}", responseContentStored);
        // Dynamically configured client
        var httpClientDynamic = httpClientFactory.CreateClient(clientNameDynamic);
        var clientCredentialsResolver = serviceProvider.GetRequiredService<IClientCredentialsResolver>();
        var accessToken = await clientCredentialsResolver.GetClientToken(
            clientNameDynamic,
            new CredentialsClientOptions
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                OidcProviderAddress = providerAddress,
                Scope = scope,
            }
        );
        httpClientDynamic.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            JsonWebTokenConstant.Bearer,
            accessToken
        );
        var responseDynamic = await httpClientDynamic.GetAsync(new Uri(new Uri(apiAddress), "/secure"));
        responseDynamic.EnsureSuccessStatusCode();
        var responseContentDynamic = await responseDynamic.Content.ReadAsStringAsync();
        logger.LogInformation("[Dynamic HttpClient] /secure response: {responseContent}", responseContentDynamic);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Caught exception");
    }
}
