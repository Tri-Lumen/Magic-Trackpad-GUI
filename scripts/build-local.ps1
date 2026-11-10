# Builds the app on this PC. Needs the .NET 8 SDK:  winget install Microsoft.DotNet.SDK.8
# Output goes to .\publish. Build log: .\build-local.log
$root = Split-Path -Parent $PSScriptRoot
Start-Transcript -Path "$root\build-local.log" -Force | Out-Null
try {
    $sdks = & dotnet --list-sdks 2>$null
    Write-Host "SDKs found: $sdks"
    if (-not ($sdks -match '^(8|9|10)\.')) { throw '.NET SDK 8 or newer not found. Install it with: winget install Microsoft.DotNet.SDK.8' }
    dotnet publish "$root\src\TrackpadCompanion.csproj" -c Release -r win-x64 -o "$root\publish"
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }
    & "$PSScriptRoot\prune-languages.ps1" -Path "$root\publish"
    Write-Host "BUILD OK: $root\publish\TrackpadCompanion.exe"
}
catch {
    ($_ | Out-String) | Write-Host
    Write-Host 'BUILD FAILED'
}
Stop-Transcript | Out-Null
