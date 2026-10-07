$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    dotnet run --project tools/AmHerb.Demo -- --reset
    if ($LASTEXITCODE -ne 0) { throw 'Demo reset failed; no successful reset has been reported.' }
} finally { Pop-Location }
