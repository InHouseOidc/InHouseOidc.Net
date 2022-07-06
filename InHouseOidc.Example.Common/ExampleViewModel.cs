// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Example.Common
{
    public class ExampleViewModel
    {
        public string? AccessToken { get; set; }
        public string? AccessTokenExpiry { get; set; }
        public string? AccessTokenJson { get; set; }
        public string? ApiResult { get; set; }
        public string? ApiResultProvider { get; set; }
        public List<System.Security.Claims.Claim>? Claims { get; set; }
        public string? IdToken { get; set; }
        public string? IdTokenExpiry { get; set; }
        public string? IdTokenJson { get; set; }
        public string? Name { get; set; }
        public string? RefreshToken { get; set; }
        public string? Role { get; set; }
        public string? SessionExpiry { get; set; }
    }
}
