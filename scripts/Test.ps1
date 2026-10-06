param([switch]$Sql, [switch]$Browser)
$ErrorActionPreference = 'Stop'
$solutionPath = Join-Path $PSScriptRoot '..\AmHerb.sln'
$resultsPath = Join-Path $PSScriptRoot '..\artifacts\test-results'
$previousSql = $env:AMHERB_RUN_SQL_TESTS
$previousBrowser = $env:AMHERB_RUN_BROWSER_TESTS
try {
    if ($Sql -or $Browser) { $env:AMHERB_RUN_SQL_TESTS = '1' }
    if ($Browser) { $env:AMHERB_RUN_BROWSER_TESTS = '1' }
    dotnet test $solutionPath --logger 'trx;LogFileName=commerce.trx' --results-directory $resultsPath
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
} finally {
    $env:AMHERB_RUN_SQL_TESTS = $previousSql
    $env:AMHERB_RUN_BROWSER_TESTS = $previousBrowser
}
