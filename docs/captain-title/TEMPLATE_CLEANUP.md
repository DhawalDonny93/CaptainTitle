# AI-SEO module removal & template hygiene

## Done (2026-10-22): the AI-SEO product was removed
- **Database**: 35 tables (projects, prompt tracking, citations, site audits, opportunities, visibility dashboards, competitors,
  content articles, user tasks, job logs ...). The generic schema is `Database_Migrations/22_generic_template_schema.sql` (50 tables); the old tables are dropped by `24_remove_ai_seo_tables.sql`.
- **Backend**: entities, DbSets, 12 controllers, 16 services + interfaces, 7 Hangfire jobs, parsers, DTOs, prompt templates, DI registrations, the SEO endpoints of `OnboardingController`/`LlmProvidersController`.
  `Program.cs` no longer registers recurring jobs. `LlmRequest`/`LlmCostTracking`/`QuotaUsageLog` keep a plain `ProjectId` column (no FK, no navigation).
- **Web app**: 56 files (27 pages, 14 SEO services, `ProjectContext`, SEO components, SEO utils). `/dashboard` now redirects to `/studio`; sign-in always lands in `/studio`; `MainLayout` is a Captain Title account shell.
- **Also removed (per the "no DB scripts" rule)**: `migrate.sh`, the unused auto-migrating `EnsureDatabaseAsync`, startup seeding, the reseed button.
- Kept on purpose: `SubscriptionPlans` quota columns (`MaxProjects`, `MaxVisibilityChecksPerMonth` ...), `QuotaUsageLogs`, the generic LLM `ContentGeneration` plumbing in `ILlmProvider`, the template's EF `Migrations/` folder (inert history; nothing applies it).

## Still SEO-flavoured (marketing/copy only - rebrand phase P6)
`LandingPage`, `Features`, `About`, `Pricing`, `OnboardingFlowNew`, `OnboardingPlans`, `NotificationDropdown` icons, and the `requiresSubscription` flag the API still returns at login (the web app ignores it).

## Template configuration the API needs to start handling requests (found by running it)
The example config does not list these; missing values throw on the first auth request:
`Google:ClientId`, `Encryption:Key`, `EncryptionKey` (LLM key vault), `SendGrid:ApiKey`, `SendGrid:FromEmail`, `Twilio:AccountSid`, `Twilio:AuthToken`, `Twilio:FromPhoneNumber`, plus `JWT:*`.
(`appsettings.example.json` names the Google block `GoogleAuth`, but the code reads `Google`.)

## Rebrand checklist (P6)
- `Program.cs`: CORS origins (`auraaiseo.netlify.app`...), DataProtection app name, Swagger title.
- `VITE_APP_NAME`, page `<title>`, favicon, `index.html` meta; AdminUI sidebar text ("BasicFramework").
- Landing page (`LandingPage.tsx`), `Login.tsx` copy (reuse the old hero: "Turn every spoken word into a visual hook."), post-login redirect -> `/studio`, nav item to `/studio`.
- `appsettings.json` `SettingKeys.AppName` seed ("MyApp").

## Hygiene / security findings in the template (pre-existing)
| Severity | Finding | Action |
|---|---|---|
| High | `AppBackendBasic.API/keys/*.xml` (ASP.NET DataProtection **private keys**) are committed | Remove from git history, add to `.gitignore`, rotate; persist keys in DB/secret store in prod |
| Medium | `AppBackendBasic.API/uploads/profiles/**` (user photos) and `waitlist_signups.jsonl` (emails) committed | Remove; git-ignore |
| Low | Tracked build output `AppBackendBasic.API/obj/**` | `git rm --cached` |
| Medium | Quota middleware / JWT middleware log token lengths & claims at Information level | Lower to Debug in prod |
| Info | Local untracked `appsettings.Production.json` exists at repo root | Confirm no real secrets before sharing the folder |
| Info | ESLint (`npm run lint`) already fails on ~150 pre-existing `any`s; `tsc` is clean | New code is lint-clean |
