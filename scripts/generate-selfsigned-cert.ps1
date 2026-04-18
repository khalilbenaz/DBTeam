<#
.SYNOPSIS
    Generates a self-signed code-signing certificate for DBTeam (dev use only).
.DESCRIPTION
    Creates a cert in CurrentUser\My, exports to dist/dev-cert.pfx (password-protected),
    and exports the public .cer for distribution.

    LIMITATIONS — self-signed:
      - SmartScreen still warns ("Unknown publisher") unless the user manually
        imports dist/dev-cert.cer into Trusted Publishers + Trusted Root CAs.
      - Not accepted by winget or the Microsoft Store.
      - Removes the "Publisher: Unknown" UAC warning on machines that trust the cert.

    For production use, provision an Azure Trusted Signing account or an
    OV/EV certificate from a public CA (Sectigo / DigiCert / SSL.com).
.PARAMETER Password
    Password for the exported .pfx. Defaults to "dbteam" (dev convenience).
.PARAMETER Subject
    Certificate subject. Defaults to "CN=DB TEAM Dev, O=Khalil Benazzouz".
.EXAMPLE
    pwsh ./scripts/generate-selfsigned-cert.ps1
    pwsh ./scripts/generate-selfsigned-cert.ps1 -Password mysecret
#>
param(
    [string]$Password = "dbteam",
    [string]$Subject  = "CN=DB TEAM Dev, O=Khalil Benazzouz, C=FR"
)

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$distDir = Join-Path $repo "dist"
New-Item -ItemType Directory -Force -Path $distDir | Out-Null

Write-Host "==> Creating self-signed code-signing certificate" -ForegroundColor Cyan
$cert = New-SelfSignedCertificate `
    -Subject $Subject `
    -Type CodeSigningCert `
    -KeyUsage DigitalSignature `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears(5) `
    -FriendlyName "DB TEAM Dev Signing"

Write-Host "   Thumbprint : $($cert.Thumbprint)"
Write-Host "   Valid until: $($cert.NotAfter)"

$pfxPath = Join-Path $distDir "dev-cert.pfx"
$cerPath = Join-Path $distDir "dev-cert.cer"
$secure  = ConvertTo-SecureString -String $Password -Force -AsPlainText

Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $secure -Force | Out-Null
Export-Certificate   -Cert $cert -FilePath $cerPath -Force             | Out-Null

Write-Host ""
Write-Host "✅ Generated" -ForegroundColor Green
Write-Host "   Private key (PFX)  : $pfxPath   password: $Password"
Write-Host "   Public cert  (CER) : $cerPath   (distribute to users who want to trust this dev build)"
Write-Host ""
Write-Host "Next: pwsh ./scripts/sign.ps1 -PfxPath `"$pfxPath`" -Password `"$Password`""
