param([string]$Email)
$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\src\AmHerb.Web\AmHerb.Web.csproj'
if ([string]::IsNullOrWhiteSpace($Email)) { $Email = Read-Host 'SuperAdmin email' }
$securePassword = Read-Host 'New SuperAdmin password (12+ characters, upper/lower/number/symbol)' -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$previousEmail = $env:AMHERB_ADMIN_EMAIL
$previousPassword = $env:AMHERB_ADMIN_PASSWORD
try {
    $env:AMHERB_ADMIN_EMAIL = $Email
    $env:AMHERB_ADMIN_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    dotnet run --project $projectPath --launch-profile http -- --migrate
    if ($LASTEXITCODE -ne 0) { throw 'Admin bootstrap failed.' }
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    $env:AMHERB_ADMIN_EMAIL = $previousEmail
    $env:AMHERB_ADMIN_PASSWORD = $previousPassword
    $securePassword.Dispose()
}
