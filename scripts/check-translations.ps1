# Fails if a translation file is missing a key, has an extra key, or changes the {placeholders} of the English text.
param([string]$Dir = (Join-Path $PSScriptRoot '..\src\Lang'))
$en = Get-Content (Join-Path $Dir 'en.json') -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
$bad = 0
foreach ($f in Get-ChildItem $Dir -Filter *.json) {
    if ($f.Name -eq 'en.json') { continue }
    $t = Get-Content $f.FullName -Raw -Encoding UTF8 | ConvertFrom-Json -AsHashtable
    foreach ($k in $en.Keys) {
        if (-not $t.ContainsKey($k)) { Write-Host "$($f.Name): missing key $k"; $bad++; continue }
        $a = ([regex]::Matches($en[$k], '\{[A-Za-z0-9]+\}') | ForEach-Object Value | Sort-Object) -join ','
        $b = ([regex]::Matches($t[$k], '\{[A-Za-z0-9]+\}') | ForEach-Object Value | Sort-Object) -join ','
        if ($a -ne $b) { Write-Host "$($f.Name): placeholders differ for $k ($a vs $b)"; $bad++ }
    }
    foreach ($k in $t.Keys) { if (-not $en.ContainsKey($k)) { Write-Host "$($f.Name): unknown key $k"; $bad++ } }
}
if ($bad) { throw "$bad translation problem(s)." }
Write-Host "Translations OK ($($en.Count) keys, $((Get-ChildItem $Dir -Filter *.json).Count) languages)."
