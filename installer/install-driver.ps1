# Downloads the Magic Trackpad 2 driver release and installs it with pnputil.
# Run by the Trackpad Companion installer, or by hand from an elevated PowerShell.
#
# The driver is a separate project (vitoplantamura/MagicTrackpad2ForWindows, GPL-2.0) and is not
# included in this repository. It is downloaded from the author's GitHub release page.
param(
    [string]$Url = 'https://github.com/vitoplantamura/MagicTrackpad2ForWindows/releases/download/v2.0/MT2FW11-20260223-MSSigned.zip',
    [string]$Log = (Join-Path $env:TEMP 'TrackpadCompanion-driver.log')
)

$ErrorActionPreference = 'Stop'
Start-Transcript -Path $Log -Force | Out-Null
$code = 0
try {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

    $work = Join-Path $env:TEMP 'trackpad-companion-driver'
    if (Test-Path $work) { Remove-Item $work -Recurse -Force }
    New-Item -ItemType Directory -Path $work | Out-Null

    Write-Host "Downloading $Url"
    $zip = Join-Path $work 'driver.zip'
    Invoke-WebRequest -Uri $Url -OutFile $zip -UseBasicParsing
    Expand-Archive -Path $zip -DestinationPath (Join-Path $work 'pkg')

    $arch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'ARM64' } else { 'AMD64' }
    $inf = Get-ChildItem (Join-Path $work 'pkg') -Recurse -Filter 'AmtPtpDevice.inf' |
        Where-Object { $_.Directory.Name -eq $arch } | Select-Object -First 1
    if (-not $inf) { throw "The download does not contain an $arch driver." }

    # Refuse to install a package whose catalog signature does not verify.
    $cat = Join-Path $inf.DirectoryName 'amtptpdevice.cat'
    $sig = Get-AuthenticodeSignature -FilePath $cat
    Write-Host "Catalog signature: $($sig.Status) ($($sig.SignerCertificate.Subject))"
    if ($sig.Status -ne 'Valid') { throw "The driver catalog signature is not valid ($($sig.Status))." }

    Write-Host "Installing $($inf.FullName)"
    & pnputil.exe /add-driver $inf.FullName /install
    # 0 = success, 3010 = success, restart required
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 3010) { throw "pnputil failed with exit code $LASTEXITCODE" }
    Write-Host 'Driver installed.'
}
catch {
    Write-Host ($_ | Out-String)
    $code = 1
}
finally {
    Stop-Transcript | Out-Null
}
exit $code
