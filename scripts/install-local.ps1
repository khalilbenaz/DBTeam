<#
.SYNOPSIS
    Installs DB TEAM on this PC without requiring Inno Setup.
.DESCRIPTION
    Copies the published output to %LocalAppData%\Programs\DBTeam,
    creates Start menu + desktop shortcuts, registers in Apps & features
    so it's uninstallable via Windows Settings.
#>
param([string]$Version = "")

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
Set-Location $repo

if (-not $Version) {
    [xml]$xml = Get-Content (Join-Path $repo "Directory.Build.props")
    $Version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
}

$source = Join-Path $repo "dist/DBTeam-$Version-win-x64"
if (-not (Test-Path $source)) {
    & "$PSScriptRoot/publish.ps1" -Version $Version
}

$installRoot = Join-Path $env:LOCALAPPDATA "Programs\DBTeam"
Write-Host "==> Installing to $installRoot" -ForegroundColor Cyan

if (Test-Path $installRoot) {
    # Stop running instance if any
    Get-Process -Name "DBTeam.App" -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill(); Start-Sleep 1 }
    Remove-Item $installRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $installRoot | Out-Null
Copy-Item "$source\*" $installRoot -Recurse -Force

$exe = Join-Path $installRoot "DBTeam.App.exe"
if (-not (Test-Path $exe)) { throw "DBTeam.App.exe not found after copy" }

# Start menu shortcut
$startMenu = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$shortcut = Join-Path $startMenu "DB TEAM.lnk"
$shell = New-Object -ComObject WScript.Shell
$sc = $shell.CreateShortcut($shortcut)
$sc.TargetPath = $exe
$sc.WorkingDirectory = $installRoot
$sc.IconLocation = "$exe,0"
$sc.Description = "Professional SQL Server IDE"
$sc.Save()
Write-Host "   Start menu shortcut: $shortcut"

# Desktop shortcut
$desktop = [Environment]::GetFolderPath("Desktop")
$dsc = $shell.CreateShortcut((Join-Path $desktop "DB TEAM.lnk"))
$dsc.TargetPath = $exe
$dsc.WorkingDirectory = $installRoot
$dsc.IconLocation = "$exe,0"
$dsc.Description = "Professional SQL Server IDE"
$dsc.Save()
Write-Host "   Desktop shortcut created"

# Register in Apps & features (per-user, no admin needed)
$regKey = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\DBTeam"
if (Test-Path $regKey) { Remove-Item $regKey -Recurse -Force }
New-Item -Path $regKey -Force | Out-Null
$uninstall = "powershell.exe -ExecutionPolicy Bypass -NoProfile -Command `"Remove-Item -Recurse -Force '$installRoot'; Remove-Item -Force '$shortcut' -ErrorAction SilentlyContinue; Remove-Item -Force '$((Join-Path $desktop 'DB TEAM.lnk'))' -ErrorAction SilentlyContinue; Remove-Item -Recurse -Force '$regKey'`""
Set-ItemProperty -Path $regKey -Name "DisplayName"     -Value "DB TEAM"
Set-ItemProperty -Path $regKey -Name "DisplayVersion"  -Value $Version
Set-ItemProperty -Path $regKey -Name "Publisher"       -Value "Khalil Benazzouz"
Set-ItemProperty -Path $regKey -Name "DisplayIcon"     -Value $exe
Set-ItemProperty -Path $regKey -Name "InstallLocation" -Value $installRoot
Set-ItemProperty -Path $regKey -Name "UninstallString" -Value $uninstall
Set-ItemProperty -Path $regKey -Name "URLInfoAbout"    -Value "https://github.com/khalilbenaz/DBTeam"
Set-ItemProperty -Path $regKey -Name "NoModify"        -Value 1 -Type DWord
Set-ItemProperty -Path $regKey -Name "NoRepair"        -Value 1 -Type DWord
Write-Host "   Registered in Apps & features (per-user)"

Write-Host ""
Write-Host "✅ DB TEAM $Version installed" -ForegroundColor Green
Write-Host "   Launch from Start menu or run: $exe"
