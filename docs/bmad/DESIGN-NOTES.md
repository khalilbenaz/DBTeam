# Technical design notes

Concrete implementation plans for items that require external dependencies (accounts, certificates, SaaS) and therefore cannot be fully executed here. Each note gives enough detail for a contributor to pick up.

---

## E13 — T-SQL Debugger

**Goal**: step-through debugging of stored procedures and ad-hoc batches.

**Recommended approach: statement-level instrumentation**
1. Parse the T-SQL with `TSql160Parser`.
2. Walk the AST, identify statement boundaries (`TSqlStatement`).
3. Inject markers around each statement:
   ```sql
   SELECT CONCAT('@@DBTEAM_DBG@', @@checkpoint_id) AS __dbteam_dbg
   -- original statement
   ```
4. Wrap in a `try/catch` block that reports `ERROR_NUMBER()/ERROR_LINE()` on failure.
5. On execute, parse result sets as they stream in; the `__dbteam_dbg` rows become virtual breakpoints.
6. Keep a side table of `@@checkpoint_id → file/line offset`.

**Breakpoint UX**
- Click in the gutter to toggle a breakpoint.
- On run, instrumentation conditionally selects a marker only for flagged lines.
- "Pause here" behavior: client opens an explicit transaction, executes up to the breakpoint, suspends on `WAITFOR DELAY` tied to a named application lock (`sp_getapplock`) the client releases via a separate connection.

**Watch panel**
- Collect variable declarations during AST walk.
- Instrument each point of interest with `SELECT @var AS __dbteam_watch_<name>`.

**Effort**: 4–6 weeks solo.

**Alternatives considered**:
- SQL Server Debugger API (legacy SSMS ≤17) — deprecated, do not use
- Extended Events (sp_statement_completed) — read-only, no true step

---

## E14 — Code signing

**Goal**: remove Windows SmartScreen warning for downloaded installers.

**Options**:

| Provider | Cost | Effort |
|---|---|---|
| **Azure Trusted Signing** | ~$10/month | Easy — Azure AD tenant + identity |
| **DigiCert / SSL.com EV** | $250–500/year | Medium — hardware token or cloud HSM |
| **Self-signed** | free | Dev only, still triggers SmartScreen |

**CI recipe (Azure Trusted Signing)**
```yaml
- name: Azure login
  uses: azure/login@v2
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

- name: Sign binary + installer
  uses: azure/trusted-signing-action@v0.4
  with:
    azure-tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    endpoint: https://eus.codesigning.azure.net/
    trusted-signing-account-name: DBTeamSigning
    certificate-profile-name: DBTeamProfile
    files-folder: dist
    files-folder-filter: exe
```
Enable after `scripts/publish.ps1` and before `scripts/build-installer.ps1` so that both the inner `DBTeam.App.exe` and the installer are signed.

---

## E15 — MSIX packaging

**Goal**: Windows Store-compatible package, sandboxed, automatic updates, app identity.

**Steps**:
1. Add a Windows Application Packaging Project (`.wapproj`).
2. Author `Package.appxmanifest`:
   - Publisher CN matching the signing cert
   - Capabilities: `runFullTrust` (WPF needs it)
   - File association for `.sql`
   - Application logos 44×44, 150×150, 310×150 (fluent square assets)
3. Build with `msbuild /t:_GenerateAppxPackage /p:AppxPackageSigningEnabled=false` in CI.
4. Output: `DBTeam-{version}.msix`.
5. Publish to Microsoft Store (requires $19 one-time developer account) or bundle in GitHub Releases alongside the Inno Setup installer.

**Relation to existing Inno Setup installer**: keep both. Inno Setup for classic Win32 install, MSIX for Store + managed enterprise.

---

## E16 — Auto-update (Velopack)

**Goal**: in-app "An update is available, restart to apply".

**Steps**:
1. `dotnet add package Velopack`
2. In `App.OnStartup`:
   ```csharp
   VelopackApp.Build().Run();
   Task.Run(async () => {
       var mgr = new UpdateManager("https://github.com/khalilbenaz/DBTeam");
       var info = await mgr.CheckForUpdatesAsync();
       if (info != null) await mgr.DownloadUpdatesAsync(info);
   });
   ```
3. Replace `scripts/publish.ps1` steps with `vpk pack` to produce `Releases/` folder.
4. In `release.yml`, upload the Velopack release folder to GitHub Release (action `velopack/upload-release`).

Required binary signing (E14) for seamless UAC prompts.

---

## E18 — Automated screenshots (FlaUI)

**Goal**: keep README images fresh on every release.

**Recipe**:
1. New project `tests/DBTeam.Screenshots` (net8.0-windows).
2. `dotnet add package FlaUI.Core FlaUI.UIA3`.
3. Test fixture:
   ```csharp
   var app = Application.Launch(@"dist\DBTeam-{ver}-win-x64\DBTeam.App.exe");
   using var automation = new UIA3Automation();
   var main = app.GetMainWindow(automation);
   main.WaitUntilEnabled();
   Save(main, "welcome.png");
   main.FindFirstDescendant(cf.ByName("New Connection")).Click();
   // ...
   ```
4. Run in CI under a virtual display; fails offline (no SQL Server). Skip integration shots, keep UX-only screens (welcome, designer, profiler empty state).
5. Push results to `docs/images/` on release tag.

---

## E20 — LocalDB integration tests

**Goal**: real-database coverage of `SqlServerConnectionService`, `SqlServerMetadataService`, `SqlServerQueryExecutionService`.

**Recipe**:
1. New project `tests/DBTeam.IntegrationTests`.
2. GitHub Actions `windows-latest` already has SQL Server Express LocalDB.
3. xUnit fixture:
   ```csharp
   public class LocalDbFixture : IAsyncLifetime {
       public string ConnectionString { get; private set; } = "";
       public async Task InitializeAsync() {
           // sqllocaldb create / start
           // then Server=(localdb)\MSSQLLocalDB;Integrated Security=true
       }
   }
   ```
4. Per test: create a fresh database `DBTeam_Test_{Guid}`, run assertions, drop DB.
5. Coverage targets: open/close connection, list dbs/tables, get columns, execute, get estimated plan.

CI step (already in `ci.yml`): `dotnet test` picks it up automatically.

---

## E22 — Accessibility audit

**Partially shipped in v1.2.0**:
- `AutomationProperties.Name` auto-bound to `ToolTip` on all Buttons via implicit style (Themes/AppStyles.xaml).
- `BrandFocusVisual` dashed blue ring for keyboard focus.

**Remaining**:
- Verify Narrator announces each menu/toolbar correctly (manual audit).
- Test Tab order in dialogs.
- Contrast ratio check with Accessibility Insights for Windows (aim WCAG 2.1 AA).
- Respect `SystemParameters.HighContrast` (swap brand brushes for high-contrast system brushes when `SystemParameters.HighContrast` is true).

---

## E23 — Session save/restore

**Shipped in v1.2.0**:
- `Services/SessionService.cs` persists open Query tabs to `%AppData%\DBTeam\session.json`
- `MainWindow.OnWindowLoaded` restores them on next launch
- `MainWindow.OnWindowClosing` saves them

**Not covered (future)**:
- Restore non-Query documents (Schema Compare, Data Compare, Diagram).
- Preserve AvalonDock layout (floating, pinned).
- "New session" menu entry to skip restore.

---

## E25 — AI assistant

**Design decisions**:
- BYO-key: user provides their own OpenAI / Anthropic / Azure OpenAI / Ollama endpoint. Never ship a pre-paid relay.
- Privacy: redact connection strings, server names (hash to first 8 chars), never send column values.

**MVP commands** (all added to Query Editor context menu):
1. **Explain this query** → send SQL + schema snapshot (tables + column names only), get explanation.
2. **Optimize this JOIN** → send SQL + `sp_helpindex` of referenced tables, get recommendations.
3. **Generate INSERTs for this table** → send columns, ask for N rows of realistic data.

**Implementation sketch**:
- New module `DBTeam.Modules.AiAssistant`
- `IAiProvider` abstraction (OpenAI, Anthropic, Ollama impls)
- Settings screen under *Tools → AI* for API key + model selection
- Key stored via DPAPI (same as connection passwords)

---

## E27 — winget manifest

**Prerequisites**: E14 (code signing) ideally, E15 (MSIX) optionally.

**Steps**:
1. Install `wingetcreate`: `winget install Microsoft.WinGetCreate`
2. `wingetcreate new` interactive — provide GitHub release URL + SHA256.
3. Produces `manifests/k/khalilbenaz/DBTeam/{version}/*.yaml`.
4. Submit PR to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).
5. Automate future versions via `wingetcreate update` in `release.yml`:
   ```yaml
   - name: Update winget manifest
     run: |
       wingetcreate.exe update khalilbenaz.DBTeam --urls ${{ steps.setup.outputs.download-url }} --version ${{ steps.ver.outputs.version }} --submit --token ${{ secrets.WINGET_PAT }}
   ```

---

## Status summary

| Issue | Status | Next action |
|---|---|---|
| #5 screenshots | 📸 awaiting manual capture | See docs/images/README.md |
| #8 Table Designer edit | ✅ v1.2 | — |
| #10 IntelliSense CTE/alias | 🔬 research needed | Parse ScriptDom `CommonTableExpression`, resolve aliases via symbol table |
| #12 Profiler plan tree | ✅ v1.2 | — |
| #13 Debugger | 📋 design doc above | Schedule 4–6 week sprint |
| #14 Code signing | 📋 design doc above | Open Azure Trusted Signing account |
| #15 MSIX | 📋 design doc above | After E14 |
| #16 Auto-update | 📋 design doc above | After E14 |
| #18 Screenshots auto | 📋 design doc above | Lower priority than E14 |
| #22 Accessibility | ✅ v1.2 partial | Finish high-contrast mode |
| #23 Session restore | ✅ v1.2 partial | Extend to non-Query tabs |
| #25 AI assistant | 📋 design doc above | Needs product decision on provider |
| #26 LocalDB tests | 📋 design doc above | Create tests/DBTeam.IntegrationTests |
| #27 winget manifest | 📋 design doc above | After E14 + first stable release |
