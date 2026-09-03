# LashesWebsite Deployment Guide

Standalone deployment reference for the "Lashes & Beauty by Ekta" public site. Follow this top to bottom — no prior context assumed.

## 1. What this is

An ASP.NET Core MVC site at `https://lashesandbeautybyekta.gesslabs.com`, hosted on the same Plesk server as the GessFlow platform. It has **no database of its own** — every piece of content (services, staff, availability, bookings) comes from calling `https://flow-api.gesslabs.com`, GessFlow's backend API (a separate repo: `github.com/ganezm/GessFlow`). If that API is down or misconfigured, this site degrades gracefully to a "Temporarily unavailable" page rather than crashing (see `SiteControllerBase.cs`).

For infrastructure shared with the rest of the GessFlow platform (Plesk login, DNS, SSL process, server IP) see the GessFlow repo's own `Application/DEPLOYMENT.md` (`github.com/ganezm/GessFlow`) — it's the single source of truth for hosting/DNS details so they don't drift out of sync between two repos. The essentials, repeated here for convenience:
- Plesk panel: `https://p4302.bom1.stableserver.net:8443`
- DNS is managed at `https://clients.hostingraja.in/dns` (**not** Plesk's own DNS Settings page — that's a disconnected, non-authoritative zone on this setup)
- SSL: Let's Encrypt via Plesk, auto-renews, nothing to do manually

## 2. Prerequisites

- .NET 10 SDK locally (`dotnet --version` → `10.x`)
- Plesk login for the HostingRaja account
- Git access to this repo

No secrets are needed to deploy this app specifically — `appsettings.Production.json` (committed, see §4) contains only a public API base URL, nothing sensitive.

## 3. Deploying an update

```bash
cd Website
dotnet publish -c Release -o ./publish
```

1. Zip the **contents** of `publish` (not the folder itself).
2. Plesk → **Websites & Domains → lashesandbeautybyekta.gesslabs.com → Files**.
3. Select **everything** currently there and **delete it** — do not extract on top of an existing deployment. A partial overwrite (some files replaced, some left stale) breaks the app with `HTTP Error 500.31 - Failed to load ASP.NET Core runtime`, because the `.dll` no longer matches its own `.deps.json`/`.runtimeconfig.json`. If you ever see that error, this is why — delete everything and redeploy fresh.
4. Upload the new zip and extract it into the now-empty document root.
5. **Dev Tools → .NET Core** → confirm **Application startup file** reads `LashesWebsite.dll`.
6. If the site doesn't pick up the new build, open `web.config` in the Code Editor and re-save it with no changes — this recycles the IIS application pool.
7. Verify: visit `https://lashesandbeautybyekta.gesslabs.com` and confirm it renders real content (services, staff, etc.), not the fallback "Temporarily unavailable" page.

## 4. Configuration

`appsettings.Production.json` (in this repo, committed — safe, no secrets):
```json
{
  "GessFlowApi": {
    "BaseUrl": "https://flow-api.gesslabs.com"
  }
}
```
Only change this if the API's domain ever changes. There's nothing else to configure — no database connection string, no API keys.

## 5. Known issue and its fix: TLS revocation-check failures

Server-to-server calls from this app to `flow-api.gesslabs.com` can fail silently (nothing shown in the browser — the site just falls back to "Temporarily unavailable"). If you enable stdout logging (Dev Tools → .NET Core → "Redirect stdout/stderr to a file" → reload the site → check the log) and see `CRYPT_E_NO_REVOCATION_CHECK`, this is a known environment quirk: this hosting network can't reach Let's Encrypt's OCSP servers to confirm the API's certificate hasn't been revoked, so .NET's default strict TLS validation fails the connection even though the certificate itself is completely valid.

The fix is already in place in [`Website/Program.cs`](Website/Program.cs) — the `HttpClient` used to call the API has `SslOptions.CertificateRevocationCheckMode = X509RevocationMode.NoCheck` set explicitly, scoped to just that one client (not applied globally). If this code is ever removed or a second `HttpClient` is added for another external call from this app on this same host, it will need the same treatment or it will hit this same silent failure.

## 6. Rollback

```bash
git log
git checkout <last-known-good-commit-hash>
```
Then repeat the publish/deploy steps in §3 from that commit. `appsettings.Production.json` is committed and holds no secrets, so checking out an older commit doesn't require any manual server-side config recovery.
