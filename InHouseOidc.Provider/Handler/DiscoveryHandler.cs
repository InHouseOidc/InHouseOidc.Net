// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Common.Constant;
using InHouseOidc.Common.Extension;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using InHouseOidc.Provider.Type;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace InHouseOidc.Provider.Handler
{
    internal class DiscoveryHandler : IEndpointHandler<DiscoveryHandler>
    {
        private readonly ProviderOptions providerOptions;

        public DiscoveryHandler(ProviderOptions providerOptions)
        {
            this.providerOptions = providerOptions;
        }

        public async Task<bool> HandleRequest(HttpRequest httpRequest)
        {
            // Only GET allowed
            if (!HttpMethods.IsGet(httpRequest.Method))
            {
                throw new BadRequestException(
                    ProviderConstant.InvalidHttpMethod,
                    "HttpMethod not supported: {method}",
                    httpRequest.Method
                );
            }
            // Write discovery properties
            var issuer = httpRequest.GetBaseUriString();
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            utf8JsonWriter.WriteStartObject();
            utf8JsonWriter.WriteNameValue(DiscoveryConstant.Issuer, issuer);
            utf8JsonWriter.WriteNameUri(
                DiscoveryConstant.JsonWebKeySetUri,
                issuer,
                this.providerOptions.JsonWebKeySetUri
            );
            if (this.providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
            {
                utf8JsonWriter.WriteNameUri(
                    DiscoveryConstant.AuthorizationEndpoint,
                    issuer,
                    this.providerOptions.AuthorizationEndpointUri
                );
            }
            if (this.providerOptions.CheckSessionEndpointEnabled)
            {
                utf8JsonWriter.WriteNameUri(
                    DiscoveryConstant.CheckSessionEndpoint,
                    issuer,
                    this.providerOptions.CheckSessionEndpointUri
                );
            }
            utf8JsonWriter.WriteNameUri(
                DiscoveryConstant.EndSessionEndpoint,
                issuer,
                this.providerOptions.EndSessionEndpointUri
            );
            utf8JsonWriter.WriteNameUri(DiscoveryConstant.TokenEndpoint, issuer, this.providerOptions.TokenEndpointUri);
            if (this.providerOptions.UserInfoEndpointEnabled)
            {
                utf8JsonWriter.WriteNameUri(
                    DiscoveryConstant.UserInfoEndpoint,
                    issuer,
                    this.providerOptions.UserInfoEndpointUri
                );
            }
            utf8JsonWriter.WriteNameValues(
                DiscoveryConstant.GrantTypesSupported,
                this.providerOptions.GrantTypes.ToStringList()
            );
            utf8JsonWriter.WriteNameValues(
                DiscoveryConstant.TokenEndpointAuthMethodsSupported,
                this.providerOptions.TokenEndpointAuthMethods
            );
            utf8JsonWriter.WriteNameValues(
                DiscoveryConstant.ScopesSupported,
                new string[]
                {
                    JsonWebTokenConstant.OpenId,
                    JsonWebTokenConstant.Address,
                    JsonWebTokenConstant.Email,
                    JsonWebTokenConstant.Phone,
                    JsonWebTokenConstant.Profile,
                    JsonWebTokenConstant.Role,
                }
            );
            utf8JsonWriter.WriteNameValues(
                DiscoveryConstant.ClaimsSupported,
                new string[] { JsonWebTokenClaim.Subject }
            );
            if (this.providerOptions.GrantTypes.Contains(GrantType.AuthorizationCode))
            {
                utf8JsonWriter.WriteNameValues(
                    DiscoveryConstant.ResponseModesSupported,
                    new string[] { DiscoveryConstant.ResponseModeQuery }
                );
                utf8JsonWriter.WriteNameValues(
                    DiscoveryConstant.ResponseTypesSupported,
                    new string[] { DiscoveryConstant.ResponseTypeCode }
                );
                utf8JsonWriter.WriteNameValues(
                    DiscoveryConstant.CodeChallengeMethodsSupported,
                    new string[] { DiscoveryConstant.S256 }
                );
            }
            utf8JsonWriter.WriteNameValues(
                DiscoveryConstant.IdTokenSigningAlgValuesSupported,
                new string[] { DiscoveryConstant.RS256 }
            );
            utf8JsonWriter.WriteNameValues(
                DiscoveryConstant.SubjectTypesSupported,
                new string[] { DiscoveryConstant.Public }
            );
            utf8JsonWriter.WriteNameValue(DiscoveryConstant.RequestParameterSupported, false);
            utf8JsonWriter.WriteEndObject();
            utf8JsonWriter.Flush();
            // Write response content
            await httpRequest.HttpContext.Response.WriteStreamJsonContent(memoryStream);
            return true;
        }
    }
}
