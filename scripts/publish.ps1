<#
.SYNOPSIS
    Publishes DB TEAM as a self-contained single-file Windows executable.
.DESCRIPTION
    Produces dist/DBTeam-{version}-win-x64/ containing the exe and assets.
    Also produces a zip archive next to it.
.PARAMETER Version
    Override the version (default: read from Directory.Build.props).
.PARAMETER Configuration
    Build configuration (default: Release).
.EXAMPLE
    pwsh ./scripts/publish.ps1
    pwsh ./scripts/publish.ps1 -Version 1.2.3
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
    $propsFile = Join-Path $repo "Directory.Build.props"
    [xml]$xml = Get-Content $propsFile
    $Version = ($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1).Trim()
}

Write-Host "==> DB TEAM publish" -ForegroundColor Cyan
Write-Host "    Version : $Version"
Write-Host "    Config  : $Configuration"
Write-Host "    Runtime : $Runtime"

$output    = Join-Path $repo "dist/DBTeam-$Version-$Runtime"
$archive   = Join-Path $repo "dist/DBTeam-$Version-$Runtime.zip"
$solution  = Join-Path $repo "DBTeam.sln"
$appProj   = Join-Path $repo "src/DBTeam.App/DBTeam.App.csproj"

if (Test-Path $output)  { Remove-Item $output  -Recurse -Force }
if (Test-Path $archive) { Remove-Item $archive -Force }

Write-Host "==> dotnet restore (with RID)" -ForegroundColor Cyan
dotnet restore $appProj -r $Runtime --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) { throw "restore failed" }

Write-Host "==> dotnet publish" -ForegroundColor Cyan
dotnet publish $appProj `
    -c $Configuration `
    -r $Runtime `
    -o $output `
    -p:IsPublishing=true `
    -p:Version=$Version `
    --no-restore `
    --nologo `
    --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) { throw "publish failed" }

# Copy extras
Copy-Item (Join-Path $repo "LICENSE")   (Join-Path $output "LICENSE.txt")   -Force
Copy-Item (Join-Path $repo "README.md") (Join-Path $output "README.md")     -Force

# Remove .pdb / .xml in Release single-file (already embedded)
Get-ChildItem $output -Recurse -Include *.pdb | Remove-Item -Force -ErrorAction SilentlyContinue

Write-Host "==> zip" -ForegroundColor Cyan
Compress-Archive -Path "$output/*" -DestinationPath $archive -CompressionLevel Optimal

$sizeMb = [math]::Round((Get-Item $archive).Length / 1MB, 2)
Write-Host ""
Write-Host "✅ Published" -ForegroundColor Green
Write-Host "   folder  : $output"
Write-Host "   archive : $archive  ($sizeMb MB)"
