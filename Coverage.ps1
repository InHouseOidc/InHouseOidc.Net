$ErrorActionPreference = "Stop"
if ((Test-Path -Path ".\Coverage"))
{
	if ((Test-Path -Path ".\Coverage\Results"))
	{
		Remove-Item .\Coverage\Results -Recurse -Force
	}
}
else
{
	New-Item -ItemType directory -Path ".\Coverage"
}
dotnet test `
	/p:Exclude="[InHouseOidc.Test.Common]*%2c[*]InHouseOidc.Common.Constant.*" `
	/p:CollectCoverage=true `
	/p:CoverletOutput=..\Coverage\Results\ `
	/p:MergeWith=..\Coverage\Results\coverage.json `
	/p:CoverletOutputFormat="json%2ccobertura" `
	-m:1 `
	InHouseOidc.Net.sln
if ($LASTEXITCODE -ne 0) 
{
	Write-Error "Test run failed"
}
if (!(Test-Path -Path ".\Coverage\Tools"))
{
	dotnet tool install dotnet-reportgenerator-globaltool --tool-path .\Coverage\Tools --ignore-failed-sources
}
.\Coverage\Tools\reportgenerator.exe -reports:.\Coverage\Results\coverage.cobertura.xml -targetdir:.\Coverage\Results\
Start-Process .\Coverage\Results\index.htm
