# Code signing — how and with what

Three paths to sign DB TEAM releases. Pick one and follow the steps.

---

## Path A — Self-signed (dev only)

**Cost**: free · **Setup**: 2 minutes · **SmartScreen**: still warns users, unless they import your `.cer` into Trusted Publishers.

Use this to validate the pipeline end-to-end and for internal dev builds.

```powershell
# 1. Generate a 5-year RSA-2048 code-signing cert, export PFX + CER
pwsh ./scripts/generate-selfsigned-cert.ps1

# 2. Publish + build installer as usual
pwsh ./scripts/publish.ps1
pwsh ./scripts/build-installer.ps1

# 3. Sign the exe + installer
pwsh ./scripts/sign.ps1 -PfxPath dist/dev-cert.pfx -Password dbteam
```

### End-user trust (per machine)

Your users have to **trust the cert once** for Windows to stop warning:

```powershell
# As admin, on the user's machine:
Import-Certificate -FilePath dev-cert.cer -CertStoreLocation Cert:\LocalMachine\TrustedPublisher
Import-Certificate -FilePath dev-cert.cer -CertStoreLocation Cert:\LocalMachine\Root
```

After that the UAC dialog shows "Publisher: DB TEAM Dev" on a blue background instead of "Unknown publisher" on yellow.

> ⚠️ Never distribute `dev-cert.pfx` publicly — the password protects the PRIVATE key.

---

## Path B — Azure Trusted Signing (recommended for open-source projects)

**Cost**: ~$9.99/month after 30-day free trial · **Setup**: 1–2 h (most time is Microsoft's identity validation, async) · **SmartScreen**: trusted immediately, no reputation warm-up.

Microsoft's managed code-signing service. Certificates are rotated every 72 h, signed in-cloud via API (no HSM to manage).

### One-time setup

1. **Azure subscription** — create one at <https://portal.azure.com> if you don't have it (credit card required).
2. **Verify your identity** — Microsoft requires either:
   - An Azure AD tenant that's already identity-verified (most are), OR
   - Manual verification by opening a support ticket with passport/articles of association.
3. **Create a Trusted Signing Account** in the portal:
   - *Create a resource* → search "Trusted Signing" → *Create*
   - Region: East US / West Europe (one of the few supported)
   - Pricing: Basic (included in trial)
4. **Create an Identity Validation** — provide the legal name that'll appear in the signed binary's *Publisher* field. Takes 1–3 business days to approve.
5. **Create a Certificate Profile** once validation is approved:
   - Type: *Public Trust*
   - Uses: *Code Signing*
   - Tie it to the validated identity
6. **Create an Azure AD App Registration** (for GitHub Actions to authenticate):
   - Azure Portal → *App registrations* → *New* → name: `DBTeam-Signing-GH`
   - Copy `Application (client) ID` + `Directory (tenant) ID`
   - *Certificates & secrets* → new client secret → copy the value **once**
7. **Grant role on the signing account**:
   - Signing Account → *Access control (IAM)* → *Add role assignment*
   - Role: *Trusted Signing Certificate Profile Signer*
   - Principal: the App Registration from step 6
8. **Add 5 GitHub secrets** to the repo:
   ```
   AZURE_TENANT_ID                → from step 6
   AZURE_CLIENT_ID                → from step 6
   AZURE_CLIENT_SECRET            → from step 6 (shown once)
   AZURE_TRUSTED_SIGNING_ACCOUNT  → name of the account (step 3)
   AZURE_CERT_PROFILE             → name of the profile (step 5)
   ```

### Release flow (nothing changes on your side)

The workflow step in `.github/workflows/release.yml` is already there and gated on `AZURE_TENANT_ID` — it activates automatically the moment the secret is set. Tag `v1.10.0`, push, done.

### Monthly ongoing cost

- First 5 signing certificates: free
- Extra certificates: $9.99 each
- Per-signing fee: 5,000 signatures/month included, then $0.005 per signature

For a 1 signed release/week pace → effectively free after the trial.

---

## Path C — Traditional certificate (OV or EV)

**Cost**: 250–800 €/year depending on type and reseller · **Setup**: 3 days (OV) to 2 weeks (EV) · **SmartScreen**: warm-up required (OV) or immediate (EV).

### OV (Organization Validated) — ~250–400 €/year

**Providers**: [Sectigo](https://sectigo.com/ssl-certificates-tls/code-signing), [SSL.com](https://www.ssl.com/certificates/code-signing/), [Certum](https://shop.certum.eu/).

**Process**:

1. **Order** an OV Code Signing certificate on the CA's website.
2. **Provide legal documents**:
   - For a company: Kbis (France) / articles of association + tax ID
   - For an individual: scan of ID + proof of address (in some countries)
3. **Validation call** — the CA calls your listed company phone (takes the slot to add your number in an authoritative directory like D&B beforehand, or the CA will reject).
4. **Hardware token** (required since June 2023 per CA/B Forum baseline) — the CA ships you a **FIPS 140-2 Level 2 YubiKey or SafeNet eToken** in 3–5 business days. The private key lives on it forever. Cost: ~50 € included in some packages.
5. **Install cert on the token** using the CA's onboarding portal.
6. **Sign** using `signtool /n "Subject CN"` — signtool finds the cert via the inserted token. For GitHub Actions, you'd need a self-hosted Windows runner with the token plugged in, which is why most OSS projects pick Azure Trusted Signing instead.

### EV (Extended Validation) — ~500–800 €/year

Same process as OV plus:

- **Stricter identity validation**: notarized documents + video call
- **SmartScreen trust is immediate** (no warm-up)
- **Kernel-mode driver signing** allowed (irrelevant here — DB TEAM is user-mode)

Unless you plan to ship Windows drivers, **OV is enough** once you've accumulated a few thousand downloads to warm SmartScreen up (happens automatically, takes 1–3 months). Before that, signed-OV binaries still trigger the SmartScreen warning even though they're correctly signed — nothing you can do about it short of paying EV or building reputation.

### Sign with OV/EV

```powershell
# With the token plugged in:
signtool sign /fd SHA256 /n "DB TEAM" /tr http://timestamp.digicert.com /td SHA256 dist\DBTeam-Setup-1.10.0.exe
```

Or, if your CA issued a PFX (some small OV plans still do despite the 2023 rule):

```powershell
pwsh ./scripts/sign.ps1 -PfxPath path/to/cert.pfx -Password <pwd>
```

---

## Recommendation for DB TEAM

1. **Now**: run `generate-selfsigned-cert.ps1` + `sign.ps1` to validate the pipeline locally. Costs nothing.
2. **First public stable release**: open an Azure account, submit identity validation, wire the 5 secrets. Total cost ≈ $10/month, zero hardware.
3. **Only consider EV** if you reach enterprise customers who ask for it explicitly. OV + Trusted Signing covers 99% of cases.

### What you get right now vs. signed

| State | Today (unsigned v1.9.0) | Self-signed (Path A) | Azure Trusted Signing (Path B) |
|---|---|---|---|
| SmartScreen download | "Éditeur inconnu" | idem unless user trusts cert | No warning |
| UAC dialog | "Éditeur: Inconnu" (yellow) | "DB TEAM Dev" (blue, if trusted) | "Khalil Benazzouz" (blue) |
| winget submission | Rejected | Rejected | Accepted |
| Auto-update (Velopack) | Fails silently | Works inside trusting users | Works for everyone |
| Enterprise GPO rollout | Blocked | Blocked | Unblocked |
