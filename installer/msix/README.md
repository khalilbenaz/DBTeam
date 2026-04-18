# MSIX packaging (scaffold)

This folder contains the skeleton of an MSIX package for DB TEAM. Finalizing it requires:

1. **Code signing certificate** (see [E14](../../docs/bmad/DESIGN-NOTES.md#e14--code-signing)): Azure Trusted Signing, EV cert, or self-signed for dev.
2. **App icons** at the required sizes in `Assets/`:
   - `StoreLogo.png` 50×50
   - `Square150x150Logo.png`
   - `Square44x44Logo.png`
   - `Wide310x150Logo.png`
3. **Build** with `makeappx` or a Windows Application Packaging Project (`.wapproj`) referencing `DBTeam.App`.

## Build command (once assets + signing cert are in place)

```powershell
# 1. Publish the unpackaged binaries
pwsh ./scripts/publish.ps1 -Version 1.7.0

# 2. Copy manifest + assets into the publish folder
Copy-Item installer/msix/Package.appxmanifest dist/DBTeam-1.7.0-win-x64/AppxManifest.xml
Copy-Item installer/msix/Assets/*              dist/DBTeam-1.7.0-win-x64/Assets/ -Recurse

# 3. Build MSIX
MakeAppx.exe pack /d dist/DBTeam-1.7.0-win-x64 /p dist/DBTeam-1.7.0.msix

# 4. Sign
signtool.exe sign /fd SHA256 /a /f cert.pfx /p <password> dist/DBTeam-1.7.0.msix
```

## Winget submission

See `installer/winget/` for the companion manifest that references the MSIX.
