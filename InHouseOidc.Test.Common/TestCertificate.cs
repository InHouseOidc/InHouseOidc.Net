// Copyright 2022 Brent Johnson.
// Licensed under the Apache License, Version 2.0 (refer to the LICENSE file in the solution folder).

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace InHouseOidc.Test.Common
{
    public static class TestCertificate
    {
        public static X509Certificate2 Create(DateTimeOffset utcNow)
        {
            var rsa = RSA.Create();
            var certificateRequest = new CertificateRequest(
                $"cn=InHouseOidc.UnitTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            return certificateRequest.CreateSelfSigned(utcNow.AddYears(-1), utcNow.AddYears(1));
        }

        public static X509Certificate2 CreateExpired(DateTimeOffset utcNow)
        {
            var rsa = RSA.Create();
            var certificateRequest = new CertificateRequest(
                $"cn=InHouseOidc.UnitTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            return certificateRequest.CreateSelfSigned(utcNow.AddYears(-2), utcNow.AddYears(-1));
        }

        public static X509Certificate2 CreateNearExpired(DateTimeOffset utcNow)
        {
            var rsa = RSA.Create();
            var certificateRequest = new CertificateRequest(
                $"cn=InHouseOidc.UnitTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            return certificateRequest.CreateSelfSigned(utcNow.AddYears(-1), utcNow.AddDays(2));
        }

        public static X509Certificate2 CreateNotReady(DateTimeOffset utcNow)
        {
            var rsa = RSA.Create();
            var certificateRequest = new CertificateRequest(
                $"cn=InHouseOidc.UnitTest",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            return certificateRequest.CreateSelfSigned(utcNow.AddYears(-2), utcNow.AddYears(-1));
        }

        public static X509Certificate2 CreatePublicOnly(DateTimeOffset utcNow)
        {
            return new X509Certificate2(Create(utcNow).Export(X509ContentType.Cert));
        }

        public static X509Certificate2 CreateNonRS256(DateTimeOffset utcNow)
        {
            var eCDsa = ECDsa.Create();
            var certificateRequest = new CertificateRequest(
                $"cn=InHouseOidc.UnitTest",
                eCDsa,
                HashAlgorithmName.SHA256
            );
            return certificateRequest.CreateSelfSigned(utcNow, utcNow.AddYears(1));
        }
    }
}
