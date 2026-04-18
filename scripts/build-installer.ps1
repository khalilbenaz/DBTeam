<#
.SYNOPSIS
    Builds the Inno Setup installer (.exe) on top of the published output.
.DESCRIPTION
    Calls scripts/publish.ps1 first, then compiles installer/DBTeam.iss via ISCC.
    Produces dist/DBTeam-Setup-{version}.exe
.NOTES
    Requires Inno Setup 6+ on PATH (ISCC.exe).
    On CI: `choco install innosetup -y` (GitHub Actions windows-latest has it available).
#>
param(
    [string]$Version = "",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $repo

if (-not $Version) {
    [xml]$xml = Get-Content (Join-Path $repo "Directory.Build.props")
    $Version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
}

# Ensure publish output exists
$published = Join-Path $repo "dist/DBTeam-$Version-$Runtime"
if (-not (Test-Path $published)) {
    & "$PSScriptRoot/publish.ps1" -Version $Version -Configuration $Configuration -Runtime $Runtime
    if ($LASTEXITCODE -ne 0) { throw "publish failed" }
}

# Locate Inno Setup Compiler
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "$(Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source)"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if (-not $iscc) {
    Write-Host "Inno Setup not found. Install with: choco install innosetup -y" -ForegroundColor Yellow
    exit 1
}

Write-Host "==> Compiling installer with $iscc" -ForegroundColor Cyan
$iss = Join-Path $repo "installer/DBTeam.iss"
& $iscc /DAppVersion=$Version /DSourceDir="dist\DBTeam-$Version-$Runtime" /DOutputDir="dist" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC failed" }

$setup = Join-Path $repo "dist/DBTeam-Setup-$Version.exe"
if (Test-Path $setup) {
    $sz = [math]::Round((Get-Item $setup).Length / 1MB, 2)
    Write-Host ""
    Write-Host "✅ Installer built" -ForegroundColor Green
    Write-Host "   $setup  ($sz MB)"
} else {
    throw "Installer not produced."
}
