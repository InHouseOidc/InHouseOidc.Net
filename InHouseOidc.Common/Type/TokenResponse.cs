// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using InHouseOidc.Common.Constant;

namespace InHouseOidc.Common.Type
{
    public class TokenResponse
    {
        [JsonPropertyName(JsonWebTokenConstant.AccessToken)]
        public string? AccessToken { get; set; }

        [JsonPropertyName(JsonWebTokenConstant.ExpiresIn)]
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public double? ExpiresIn { get; set; }

        [JsonPropertyName(JsonWebTokenConstant.IdToken)]
        public string? IdToken { get; set; }

        [JsonPropertyName(JsonWebTokenConstant.RefreshToken)]
        public string? RefreshToken { get; set; }

        [JsonPropertyName(JsonWebTokenConstant.SessionState)]
        public string? SessionState { get; set; }

        [JsonPropertyName(JsonWebTokenConstant.TokenType)]
        public string? TokenType { get; set; }
    }
}
