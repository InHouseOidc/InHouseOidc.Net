// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common;
using InHouseOidc.Provider.Constant;
using InHouseOidc.Provider.Exception;
using InHouseOidc.Provider.Extension;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace InHouseOidc.Provider.Handler
{
    internal class JsonWebKeySetHandler : IEndpointHandler<JsonWebKeySetHandler>
    {
        private readonly ISigningKeyHandler signingKeyHandler;

        public JsonWebKeySetHandler(ISigningKeyHandler signingKeyHandler)
        {
            this.signingKeyHandler = signingKeyHandler;
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
            // Write json web key properties
            using var memoryStream = new MemoryStream();
            using var utf8JsonWriter = new Utf8JsonWriter(memoryStream, JsonHelper.JsonWriterOptions);
            utf8JsonWriter.WriteStartObject();
            utf8JsonWriter.WritePropertyName(JsonWebKeySetConstant.Keys);
            utf8JsonWriter.WriteStartArray();
            foreach (var signingKey in await this.signingKeyHandler.Resolve())
            {
                // Write the signing key to the json array
                utf8JsonWriter.WriteStartObject();
                utf8JsonWriter.WriteNameValue(JsonWebKeySetConstant.Kid, signingKey.JsonWebKey.Kid);
                utf8JsonWriter.WriteNameValue(JsonWebKeySetConstant.Use, signingKey.JsonWebKey.Use);
                utf8JsonWriter.WriteNameValue(JsonWebKeySetConstant.Kty, signingKey.JsonWebKey.Kty);
                utf8JsonWriter.WriteNameValue(JsonWebKeySetConstant.Alg, signingKey.JsonWebKey.Alg);
                utf8JsonWriter.WriteNameValue(JsonWebKeySetConstant.E, signingKey.JsonWebKey.E);
                utf8JsonWriter.WriteNameValue(JsonWebKeySetConstant.N, signingKey.JsonWebKey.N);
                utf8JsonWriter.WriteNameValue(JsonWebKeySetConstant.X5t, signingKey.JsonWebKey.X5t);
                utf8JsonWriter.WriteNameValues(JsonWebKeySetConstant.X5c, signingKey.JsonWebKey.X5c);
                utf8JsonWriter.WriteEndObject();
            }
            utf8JsonWriter.WriteEndArray();
            utf8JsonWriter.WriteEndObject();
            utf8JsonWriter.Flush();
            // Write response content
            await httpRequest.HttpContext.Response.WriteStreamJsonContent(memoryStream);
            return true;
        }
    }
}
