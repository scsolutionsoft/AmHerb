param([ValidateRange(120,500)][int]$Orders = 180)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\tools\AmHerb.Demo\AmHerb.Demo.csproj'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    dotnet run --project $project -- --seed --orders $Orders
    if ($LASTEXITCODE -ne 0) { throw 'Demo creation failed. Inspect the error, then reset the incomplete demo before reseeding.' }
    Write-Output 'Examples and credentials: artifacts/demo/GUIDE.md'
    Write-Output 'Start the web app with: dotnet run --project src/AmHerb.Web --no-build --launch-profile http'
} finally { Pop-Location }
