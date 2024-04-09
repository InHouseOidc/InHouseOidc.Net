// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using Microsoft.IdentityModel.Tokens;

namespace InHouseOidc.Common
{
    public static class HashHelper
    {
        public static string HashCodeVerifierS256(string codeVerifier)
        {
            var codeVerifierHashed = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64UrlEncoder.Encode(codeVerifierHashed);
        }

        public static string GenerateCode()
        {
            return GenerateHash(64);
        }

        public static string GenerateSessionId()
        {
            return GenerateHash(16);
        }

        public static string GenerateSessionState(
            string? salt,
            string clientId,
            string redirectUriString,
            string sessionId
        )
        {
            if (string.IsNullOrEmpty(salt))
            {
                salt = GenerateHash(16);
            }
            var redirectUri = new Uri(redirectUriString);
            var sessionStateBytes = Encoding.UTF8.GetBytes(
                $"{clientId}{redirectUri.GetLeftPart(UriPartial.Authority)}{sessionId}{salt}"
            );
            var sessionStateHashed = SHA256.HashData(sessionStateBytes);
            return $"{Base64UrlEncoder.Encode(sessionStateHashed)}.{salt}";
        }

        private static string GenerateHash(int length)
        {
            var hashBytes = new byte[length];
            using var randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(hashBytes);
            return Base64UrlEncoder.Encode(hashBytes);
        }
    }
}
