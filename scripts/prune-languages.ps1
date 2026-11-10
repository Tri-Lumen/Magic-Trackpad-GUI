# Removes the Windows App SDK's translated resource folders (pt-BR, de-DE, ...) from a publish folder.
# The app UI is English only, so they are dead weight; Windows falls back to en-US.
param([Parameter(Mandatory)][string]$Path)
$keep = @('en', 'en-US')
$removed = 0
Get-ChildItem -LiteralPath $Path -Directory |
    Where-Object { $_.Name -match '^[a-z]{2,3}(-[A-Za-z0-9]+)*$' -and $keep -notcontains $_.Name } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force; $removed++ }
$mb = [math]::Round(((Get-ChildItem -LiteralPath $Path -Recurse -File | Measure-Object Length -Sum).Sum) / 1MB, 1)
Write-Host "Removed $removed language folders. Publish folder is now $mb MB."
