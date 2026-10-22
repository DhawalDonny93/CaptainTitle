# Migration status

_Last updated: 2026-10-21. Old baseline: commit `4ee2eea` ("Wire SFX markers and template variants into the actual video export")._

## Verification summary (be skeptical of anything not in the "verified" column)

| Area | Verified how | Not verified |
|---|---|---|
| Transcript stitching, alignment, chunking, repetition guards, ASS/SRT writers, ffmpeg arguments, seeded themes/fonts/variants/SFX | 13 golden tests: C# output == original Python output (81 ASS scripts, all themes x variants x layouts) | - |
| EF model vs SQL scripts | `SchemaTests` (tables+columns of EF model == scripts 22+21; no AI-SEO tables; script 24 drops exactly the removed tables) + `CaptionSeedSqlTests` | Scripts 21-24 ran on a **local PostgreSQL 18** (fresh DB, re-run, and upgrade of the pre-change schema with data + mock Hangfire) - **not on your Supabase project** |
| Backend compile | `dotnet build` of API/AdminUI/Services/Tests = 0 errors | App has **not been started** (no DB/keys here) |
| Frontend | `tsc` 0 errors, ESLint clean on new code, `vite build` OK, studio code-split | **Not opened in a browser**; no UI/E2E test |
| ffmpeg render / preview / probe | Argument lists golden-tested | ffmpeg is **not installed on this machine** - no real render executed |
| Whisper / Sarvam / OpenAI / Stripe | Request shapes follow vendor docs (Sarvam contract fetched 2026-10-21) | **No live calls made** |
| Running API | Started against a database built ONLY from scripts 22+21 and called over HTTP: catalog, register/login, usage, payments error paths, admin catalog CRUD (+validation), credit grants, project save/validation, SRT, signed media URL with HTTP Range (206), job failure/resume paths | Admin UI pages (Razor compiles, not clicked); ffmpeg/Whisper/Sarvam/LLM/Stripe happy paths |

## Phase plan

- [x] **P0 Recon** - read all old backend/frontend + template conventions
- [x] **P1 Tracking** - docs, port-map, baseline manifest, change-detector script, golden generator
- [x] **P2 Backend domain** - entities, DTOs, SQL, catalog, entitlements, pipeline, render, billing, queue, controllers, admin API
- [x] **P3 Frontend port** - full editor (all 7 panels, timeline, moment popover, transcript, paywall, payment result)
- [x] **P4 Admin controls** - Captain Title section in AdminUI (catalogs, settings, projects, credits)
- [ ] **P5 First real run** (needs your environment): apply SQL, set keys, install ffmpeg, upload a clip, render, compare with the old app output (see checklist below)
- [ ] **P6 Rebrand surfaces**: landing page, login copy/branding, nav, post-login redirect to `/studio`, app name/CORS/DataProtection name (see TEMPLATE_CLEANUP.md)
- [x] **P7 Remove AI-SEO template modules** (done 2026-10-22 - code + SQL scripts 22/24; see TEMPLATE_CLEANUP.md)
- [ ] **P8 Hardening backlog** (below)

## P5 first-run checklist

1. Run the SQL scripts by hand in the order in `Database_Migrations/RUN_ORDER.md` (23 check → 22 → 21 → deploy → 24).
2. Copy `AppBackendBasic.API/appsettings.example.json` -> `appsettings.Development.json`; set `ConnectionStrings`, `JWT`, `Captions:SarvamApiKey`, `Stripe` (optional).
3. Install ffmpeg (needs libx264, libvpx-vp9, libopus, libass): `brew install ffmpeg`.
4. Start API; sign in to AdminUI as Admin -> LLM Providers: add the **OpenAI API key** (used for Whisper timing + Hinglish).
5. `cd AppBackendBasic.WebUI && npm ci && npm run dev` -> sign up -> open `/studio`.
6. Upload a short MP4 (<45 s) -> captions -> edit -> Export -> compare against the old app on the same clip (`flying-beast-source.mp4`, `toe-protector-source.mp4` in the old repo are good fixtures).
7. Confirm `/api/captions/media/{id}` seeking works in the browser (Range requests).

## Backlog (carried from old PRD + found during port)

| Pri | Item | Source |
|---|---|---|
| P0 | India payment route: Razorpay or Stripe with supported business country (template already references Razorpay) | old PRD P0 |
| P0 | Keep `captions.unlimited_testing` = false in production | old PRD P0 |
| P1 | Subscription renewals/cancellation: old code only handled `checkout.session.completed`, so Basic credits are granted once and never refilled/revoked. Add `invoice.paid` / `customer.subscription.deleted` handling | found in port |
| P1 | Project history list + reopen past sessions (backend: add `GET /api/captions/projects`) | old PRD |
| P1 | Higher-accuracy timestamp-capable speech provider | old PRD |
| P1 | Real object storage (S3/R2) behind `ICaptionStorage`; delete old uploads/renders (retention job) | old README |
| P1 | Admin UI shows only generic failure text; surface API error messages | found in port |
| P2 | In-editor trimming for 60-120 s videos; custom font upload; SRT/VTT variants; brand kits | old PRD |
| P2 | Editor/export constants duplicated in TS + C# (grouping 0.35 s/3 words/0.4 s gap): expose via catalog limits | found in port |
| P2 | Live API integration tests (WebApplicationFactory + Testcontainers Postgres + faked Whisper/Sarvam/LLM) | replaces old pytest smoke tests |
| P2 | Kinetic animations / box sizes are editor-only (not in export) - same limitation as old app | inherited |
