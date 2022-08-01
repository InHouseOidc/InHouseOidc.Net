$ErrorActionPreference = "Stop"
$filename = "$PWD\\InHouseOidcExample.pfx"
if (Test-Path $filename) {
    Remove-Item $filename
}
$rsa = [System.Security.Cryptography.RSA]::Create()
$hashAlgorithm = [System.Security.Cryptography.HashAlgorithmName]::SHA256
$rsaSignaturePadding = [System.Security.Cryptography.RSASignaturePadding]::Pkcs1
$cr = New-Object System.Security.Cryptography.X509Certificates.CertificateRequest("cn=InHouseOidc", $rsa, $hashAlgorithm, $rsaSignaturePadding)
$certificate = $cr.CreateSelfSigned([System.DateTime]::UtcNow, [System.DateTime]::UtcNow.AddYears(5))
$certificateData = $certificate.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, "Internal")
[System.IO.File]::WriteAllBytes($filename, $certificateData)
