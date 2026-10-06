$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\src\AmHerb.Web\AmHerb.Web.csproj'
$outputPath = Join-Path $PSScriptRoot '..\artifacts\publish'
dotnet publish $projectPath -c Release -r win-x64 --self-contained false -o $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Write-Output "IIS publish files: $outputPath"
