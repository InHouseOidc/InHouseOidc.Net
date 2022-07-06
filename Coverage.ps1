If (!(Test-Path -Path ".\Coverage"))
{
	New-Item -ItemType directory -Path ".\Coverage"
}
else
{
	Remove-Item .\Coverage\Results -Recurse -Force
}
dotnet test `
	/p:Exclude="[InHouseOidc.Test.Common]*%2c[*]InHouseOidc.Common.Constant.*" `
	/p:CollectCoverage=true `
	/p:CoverletOutput=..\Coverage\Results\ `
	/p:MergeWith=..\Coverage\Results\coverage.json `
	/p:CoverletOutputFormat="json%2copencover" `
	-m:1 `
	InHouseOidc.sln
If (!(Test-Path -Path ".\Coverage\Tools"))
{
	dotnet tool install dotnet-reportgenerator-globaltool --tool-path .\Coverage\Tools --ignore-failed-sources
}
.\Coverage\Tools\reportgenerator.exe -reports:.\Coverage\Results\coverage.opencover.xml -targetdir:.\Coverage\Results\
Start-Process .\Coverage\Results\index.htm
