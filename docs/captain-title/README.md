# Captain Title - conversion docs

Captain Title is a Hinglish word-by-word caption generator + editor (SaaS). It was first built as a FastAPI + MongoDB + React (CRA) app
(`CaptainTitle_Old/captain-title`, baseline commit in [old-baseline/BASELINE_COMMIT](old-baseline/BASELINE_COMMIT)). This repository
re-implements it on the SaaS template stack: **.NET 8 API + EF Core/PostgreSQL, in-process job queue, Razor Pages AdminUI, React 18 + Vite + TypeScript + Tailwind**.

| Doc | Purpose |
|---|---|
| [MIGRATION_STATUS.md](MIGRATION_STATUS.md) | Progress tracker, TODO plan, what is verified vs not |
| [FEATURE_PARITY.md](FEATURE_PARITY.md) | Every old feature -> where it lives now -> status |
| [ADMIN_CONTROLS.md](ADMIN_CONTROLS.md) | **What you can change, and where** (Admin UI / config / code) |
| [CUSTOM_CODE_REGISTER.md](CUSTOM_CODE_REGISTER.md) | Every hardcoded value and custom piece of code, with location + how to change |
| [DECISIONS.md](DECISIONS.md) | Deliberate deviations from the old app and old-app bugs found |
| [CONFIGURATION.md](CONFIGURATION.md) | Setup, config keys (old env var -> new key), run instructions |
| [OLD_SYNC_LOG.md](OLD_SYNC_LOG.md) | How to carry future old-project updates (and deletions) into this repo + ledger |
| [TEMPLATE_CLEANUP.md](TEMPLATE_CLEANUP.md) | Leftover AI-SEO template code to remove, and template hygiene/security findings |
| [PRODUCT_REQUIREMENTS.md](PRODUCT_REQUIREMENTS.md) | The old PRD, unchanged |

Code map (all new code is isolated so the template can still be synced):

- Backend entities/DTOs: `AppBackendBasic.Core/Entities/Caption*.cs`, `Core/DTOs/CaptionDtos.cs`
- Backend logic: `AppBackendBasic.Services/Captions/*`
- API: `AppBackendBasic.API/Controllers/Caption*Controller.cs`, `AdminCaptionsController.cs`
- DB: `Database_Migrations/21_captain_title_captions.sql` (+ `ApplicationDbContext.ConfigureCaptionEntities`)
- Admin: `AppBackendBasic.AdminUI/Pages/Captions/*`
- Frontend: `AppBackendBasic.WebUI/src/features/captions/*` (routes `/studio`, `/payment/success`, `/payment/cancel`)
- Tests: `AppBackendBasic.Captions.Tests` (golden parity against the original Python + schema guard)
- Tooling: `scripts/old-sync/*`
