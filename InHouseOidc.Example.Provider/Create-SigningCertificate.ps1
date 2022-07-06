$ErrorActionPreference = "Stop"
if (Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -eq "CN=InHouseOidcExample"})
{
	Write-Host "Local certificate InHouseOidcExample already exists" -ForegroundColor Green
}
else
{
	New-SelfSignedCertificate -Subject "InHouseOidcExample" -DnsName "InHouseOidcExample" `
		-CertStoreLocation Cert:\LocalMachine\My `
		-HashAlgorithm "SHA256" `
		-KeyExportPolicy Exportable `
		-KeyUsage DigitalSignature, KeyEncipherment `
		-NotAfter (Get-Date).AddYears(5)
	Write-Host "Added local certificate InHouseOidcExample" -ForegroundColor Yellow
}
$certificate = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -eq "CN=InHouseOidcExample"}
$securePass = ConvertTo-SecureString -String "Internal" -Force -AsPlainText
Export-PfxCertificate -Cert $certificate -FilePath ".\InHouseOidcExample.pfx" -Password $securePass
