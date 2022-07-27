# InHouseOidc.Net

![main branch](https://github.com/InHouseOidc/InHouseOidc.Net/actions/workflows/main.yml/badge.svg)
![coverage](https://raw.githubusercontent.com/InHouseOidc/InHouseOidc.Net/badges/.badges/main/coverage.svg?branch=main)
![release branch](https://github.com/InHouseOidc/InHouseOidc.Net/actions/workflows/release.yml/badge.svg)
![release](https://raw.githubusercontent.com/InHouseOidc/InHouseOidc.Net/badges/.badges/release.svg?branch=main)

# Use Case

Open ID Connect based authentication of .NET web client applications and .NET web APIs against a .NET identity provider, where all are developed in-house.

- Simplify the implementation of the moving parts of authenticated applications by using matching packages for the provider, APIs and clients
- Provide configuration defaults that ensure interoperability and represent accepted best practice
- Focus only on the single best practice authentication method available for each authentication scenario

# Commitment

- Always open source and always free to use for all individuals and all organizations
- Keep pace with best practice authentication as standards evolve
- Never place restrictions on features based on sponsorship
- Obtain and maintain OpenID Certification
- Maintain support for package versions that track .NET supported versions

# Open ID Connect Supported Flows & Features

- Authorization code with PKCE flow
- Client credentials flow
- Check session / end session endpoints (session management/single logout)
- Discovery endpoint
- UserInfo endpoint
- Refresh tokens

# Packages

Packages currently only support .NET 6.0 upwards.

The Provider, Api and PageClient packages reference the Microsoft.AspNetCore.App framework as they are expected to be used exclusively with Asp.Net projects.

The CredentialsClient package references the Microsoft.NETCore.App framework so it suitable for use in any .NET project.

### InHouseOidc.Provider

The engine room of OIDC, the provider is the server side of all the authentication flows.  To implement a provider, you must provide data to support validation in
the flows by implementing the following interfaces:

- IClientStore - details for valid clients
- ICodeStore - persistence of codes used while flows are active
- IResourceStore - allows resolution of audiences from scopes
- IUserStore - details for valid users

### InHouseOidc.Api

A simple wrapper around the .NET JwtBearer authentication. 

### InHouseOidc.CredentialsClient

The client credentials flow is suitable where secrets can be used to secure access.  HttpClient headers are automatically added and
tokens automatically renewed. 

### InHouseOidc.PageClient

The PageClient uses the Authorization Code + PKCE flow to secure web applications that use MVC and Razor pages.  AccessTokens issued during
authentication can be automatically renewed using refresh tokens.

# Examples

Please refer to the example projects for each of the packages and use cases.

- InHouseOidc.Example.Api - how to use the InHouseOidc.Api package
- InHouseOidc.Example.CredentialsClient - how to use the InHouseOidc.CredentialsClient package
- InHouseOidc.Example.Mvc - how to use the InHouseOidc.PageClient package in an MVC application
- InHouseOidc.Example.Provider - how to implement a provider using the InHouseOidc.Provider package
- InHouseOidc.Example.Razor - how to use the InHouseOidc.PageClient in a Razor Page application

Many of the examples rely on each other to operate, so please start by launching all of the examples together using the "Set Startup Projects..." / "Multiple startup projects" options.

# Testing

All of the package assemblies have 100% unit test line and branch coverage.

The InHouseOidc.Certify.Provider project has been tested against the excellent certification toolkit provided by the OpenID Foundation and will form the basis
for a future application for certification.  Note, that packages are *not* currently certified.

The Api, ClientCredentials and Provider packages are currently used by the authors own SAAS projects, successfully replacing IdentityServer4 and interoperating
successfully with SPA applications utilizing oidc-client-js.

# Contributing

Contributions are welcome.  Ensure that you:

- Have read and understand the "Use Case" and "Commitment" sections above
- Can code using C# 10 with ```<Nullable>enable</Nullable>```
- Are familiar with the build action tooling that checks code style (csharpier & dotnet format)
- Use branches prefixed with "issue/" if you need build actions to run with each commit
- Ensure all pull requests to the main branch reference an issue number

# Feedback

For any authentication package best practice is a moving target so feedback is essential.
Please feel free to discuss anything where your experience of best practice differs from the current implementation.

Please do not use Github issues for general assistance, use Github Discussions instead.  Any questions raised as issues will be tagged and closed.
