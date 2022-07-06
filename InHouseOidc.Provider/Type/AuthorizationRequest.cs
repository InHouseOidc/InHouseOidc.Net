// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

namespace InHouseOidc.Provider.Type
{
    internal class AuthorizationRequest
    {
        public AuthorizationRequest(string clientId, string redirectUri, ResponseType responseType, string scope)
        {
            this.AuthorizationRequestClaims = new();
            this.ClientId = clientId;
            this.RedirectUri = redirectUri;
            this.ResponseType = responseType;
            this.Scope = scope;
        }

        public List<AuthorizationRequestClaim> AuthorizationRequestClaims { get; set; }
        public string ClientId { get; set; }
        public string? CodeChallenge { get; set; }
        public CodeChallengeMethod? CodeChallengeMethod { get; set; }
        public string? IdTokenHint { get; set; }
        public int? MaxAge { get; set; }
        public string? Nonce { get; set; }
        public Prompt? Prompt { get; set; }
        public string RedirectUri { get; set; }
        public ResponseMode? ResponseMode { get; set; }
        public ResponseType ResponseType { get; set; }
        public string Scope { get; set; }
        public DateTimeOffset? SessionExpiryUtc { get; set; }
        public string? SessionState { get; set; }
        public string? State { get; set; }

        internal string GetClaimValue(string claimType)
        {
            var claim = this.AuthorizationRequestClaims.Single(c => c.Type == claimType);
            return claim.Value;
        }
    }
}
