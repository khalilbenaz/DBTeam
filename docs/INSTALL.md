# Install DB TEAM

## Requirements

- **Windows 10 22H2** or later (Windows 11 recommended)
- **x64** CPU
- No .NET install required — the binary is self-contained

## Download

1. Go to the [Releases page](https://github.com/khalilbenaz/DBTeam/releases).
2. Download the latest **`DBTeam-<version>-win-x64.zip`** asset.
3. Extract the archive anywhere (e.g. `C:\Tools\DBTeam\`).
4. Run **`DBTeam.App.exe`**.

> Windows SmartScreen may warn that the publisher is unverified (the binary is not yet code-signed). Click **More info → Run anyway**. Code signing is planned for v1.1.

## First run

1. **File → New Connection** (or `Ctrl+N`).
2. Fill server, auth mode, credentials.
3. Click **Load** to list databases, select one.
4. Click **Test** to verify, then **Connect**.
5. The Object Explorer opens automatically — expand your database and double-click a table for a quick `SELECT TOP 100`.

## Configuration files

DB TEAM stores user settings under your profile:

| File | Role |
|---|---|
| `%AppData%\DBTeam\connections.json` | Saved connections (passwords encrypted with Windows DPAPI) |
| `%AppData%\DBTeam\theme.json` | Light / Dark / System preference |
| `%AppData%\DBTeam\lang.json` | en-US / fr-FR preference |
| `%LocalAppData%\DBTeam\logs\app-YYYYMMDD.log` | Daily rolling log |

## Uninstall

The portable build is not registered in *Apps & features*. To remove it:

1. Delete the folder you extracted.
2. Optional: delete `%AppData%\DBTeam\` and `%LocalAppData%\DBTeam\` to clear saved settings.

## Build from source

```bash
git clone https://github.com/khalilbenaz/DBTeam.git
cd DBTeam
dotnet build DBTeam.sln
dotnet run --project src/DBTeam.App
```

Or produce your own self-contained build:

```powershell
pwsh ./scripts/publish.ps1
```
