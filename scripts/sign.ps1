<#
.SYNOPSIS
    Signs DBTeam.App.exe and DBTeam-Setup-<ver>.exe using a PFX certificate.
.DESCRIPTION
    Works with either:
      - Dev self-signed cert from scripts/generate-selfsigned-cert.ps1
      - Production cert (OV / EV) exported as PFX
    Uses signtool.exe from the Windows SDK. On CI, install via
    'windows-sdk' chocolatey package or use Azure Trusted Signing directly
    (already wired in .github/workflows/release.yml).
.PARAMETER PfxPath
    Path to the .pfx containing the private key.
.PARAMETER Password
    PFX password.
.PARAMETER Version
    App version (used to locate the produced installer).
.PARAMETER TimestampUrl
    RFC 3161 timestamp server. Default: DigiCert's free timestamper.
#>
param(
    [Parameter(Mandatory)][string]$PfxPath,
    [Parameter(Mandatory)][string]$Password,
    [string]$Version      = "",
    [string]$Runtime      = "win-x64",
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $repo

if (-not $Version) {
    [xml]$xml = Get-Content (Join-Path $repo "Directory.Build.props")
    $Version  = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
}

# Locate signtool
$signtool = @(
    "$env:ProgramFiles(x86)\Windows Kits\10\bin\x64\signtool.exe",
    "$env:ProgramFiles\Windows Kits\10\bin\x64\signtool.exe",
    (Get-ChildItem "$env:ProgramFiles(x86)\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName -First 1)
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if (-not $signtool) {
    Write-Host "signtool.exe not found. Install the Windows 10 SDK:" -ForegroundColor Yellow
    Write-Host "   choco install windows-sdk-10-version-2004-all -y" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $PfxPath)) { throw "PFX not found: $PfxPath" }

$exe = Join-Path $repo "dist/DBTeam-$Version-$Runtime/DBTeam.App.exe"
$zip = Join-Path $repo "dist/DBTeam-$Version-$Runtime.zip"
$setup = Join-Path $repo "dist/DBTeam-Setup-$Version.exe"

function Sign([string]$target) {
    if (-not (Test-Path $target)) { Write-Host "  skipped (not found): $target" -ForegroundColor DarkGray; return }
    Write-Host "==> Signing $target" -ForegroundColor Cyan
    & $signtool sign /fd SHA256 /f $PfxPath /p $Password /tr $TimestampUrl /td SHA256 /v $target
    if ($LASTEXITCODE -ne 0) { throw "signtool failed on $target" }
}

Sign $exe
Sign $setup
# Re-zip after signing the inner exe so the zip carries the signed binary
if (Test-Path (Split-Path $exe -Parent)) {
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path "$(Split-Path $exe -Parent)\*" -DestinationPath $zip -CompressionLevel Optimal
    Write-Host "   re-packaged: $zip"
}

Write-Host ""
Write-Host "✅ Signed" -ForegroundColor Green
& $signtool verify /pa /v $exe   | Select-Object -Last 5
& $signtool verify /pa /v $setup | Select-Object -Last 5
