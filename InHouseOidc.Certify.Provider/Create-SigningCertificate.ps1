$ErrorActionPreference = "Stop"
if (Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -eq "CN=InHouseOidcCertify"})
{
	Write-Host "Local certificate InHouseOidcCertify already exists" -ForegroundColor Green
}
else
{
	New-SelfSignedCertificate -Subject "InHouseOidcCertify" -DnsName "InHouseOidcCertify" `
		-CertStoreLocation Cert:\LocalMachine\My `
		-HashAlgorithm "SHA256" `
		-KeyExportPolicy Exportable `
		-KeyUsage DigitalSignature, KeyEncipherment `
		-NotAfter (Get-Date).AddYears(5)
	Write-Host "Added local certificate InHouseOidcCertify" -ForegroundColor Yellow
}
$certificate = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -eq "CN=InHouseOidcCertify"}
$securePass = ConvertTo-SecureString -String "Internal" -Force -AsPlainText
Export-PfxCertificate -Cert $certificate -FilePath ".\InHouseOidcCertify.pfx" -Password $securePass
