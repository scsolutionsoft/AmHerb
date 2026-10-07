$ErrorActionPreference = 'Stop'
$destination = Join-Path $PSScriptRoot '../src/AmHerb.Web/Data/Addresses'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$tree = Invoke-RestMethod 'https://api.github.com/repos/open-admin-data/thailand-administrative-divisions/git/trees/main?recursive=1'
$base = "https://raw.githubusercontent.com/open-admin-data/thailand-administrative-divisions/$($tree.sha)"
$rows = [Collections.Generic.List[object]]::new()
foreach ($file in @('all-provinces.json','all-districts.json','all-subdistricts.json')) {
    $data = Invoke-RestMethod "$base/data/$file"
    foreach ($item in $data) { $rows.Add([pscustomobject][ordered]@{ Id=[string]$item.id; Name=[string]$item.name.th; Parent=[string]$item.parent.id; Level=[int]$item.level; Postcodes=@($item.zip_codes | Where-Object { $_ }) }) }
}
foreach ($file in ($tree.tree | Where-Object { $_.path -like 'data/villages-by-province/*.json' })) {
    $data = Invoke-RestMethod "$base/$($file.path)"
    foreach ($item in $data) { $rows.Add([pscustomobject][ordered]@{ Id=[string]$item.id; Name=[string]$item.name.th; Parent=[string]$item.parent.id; Level=4; Postcodes=@() }) }
    Write-Output "Imported $($file.path)"
}
$seen = [Collections.Generic.HashSet[string]]::new()
$unique = @(foreach ($row in $rows) { if ($seen.Add($row.Id)) { $row } })
Write-Output "Provinces=$(@($unique | Where-Object Level -eq 1).Count); villages=$(@($unique | Where-Object Level -eq 4).Count)"
if (@($unique | Where-Object Level -eq 1).Count -ne 77) { throw 'Incomplete province dataset' }
[IO.File]::WriteAllText((Join-Path $destination 'thailand.json'), (ConvertTo-Json -InputObject $unique -Depth 5 -Compress), [Text.UTF8Encoding]::new($false))
Invoke-WebRequest "$base/LICENSE" -UseBasicParsing -OutFile (Join-Path $destination 'LICENSE.txt')
$notice = "Source: https://github.com/open-admin-data/thailand-administrative-divisions`nCommit: $($tree.sha)`nLicense: CC BY 4.0 https://creativecommons.org/licenses/by/4.0/`nCopyright (c) 2026 jakkrapongt`nAdaptation: selected Thai names, identifiers, parent relationships and postal codes; flattened and deduplicated. Imported $(Get-Date -Format yyyy-MM-dd).`nRows: $($unique.Count)"
[IO.File]::WriteAllText((Join-Path $destination 'SOURCE.txt'), $notice, [Text.UTF8Encoding]::new($false))
Write-Output "Saved $($unique.Count) address records"
