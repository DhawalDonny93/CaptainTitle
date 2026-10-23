# Decisions & deviations

| ID | Decision | Reason |
|---|---|---|
| D-01 | **Auth**: template JWT/refresh replaces cookie sessions + bcrypt + Emergent/Google redirect | User asked to follow template stack; avoids two auth systems |
| D-02 | **Mongo -> PostgreSQL**; words/elements/SFX stay as `jsonb` on `CaptionProjects` | Same document shape, no data model churn; rule: always ship Postgres SQL |
| D-03 | **Job execution**: in-process `Channel` queue (`MaxConcurrentJobs`=2) instead of Hangfire | Hangfire redelivery after a crash could double-run a job (and double-charge); the old code also ran in-process and resumed on start. Single-node limitation documented |
| D-04 | Caption API keeps **snake_case** JSON (`font_size`, `pos_x`...) via `[JsonPropertyName]` | Ported editor + stored jsonb depend on it; avoids rename bugs. Other template endpoints stay camelCase |
| D-05 | **Media auth** via signed short-lived URL (`/api/captions/media/{id}?t=`) | `<video>` can't send bearer tokens; localStorage JWT. Token is bound to project+user, 2 h |
| D-06 | Whisper/Sarvam called by **HTTP clients**, not SDKs; OpenAI key comes from Admin ▸ LLM Providers | No .NET SDK for Sarvam; reuse template key vault |
| D-07 | Stripe **return origin allow-list** (`Captions:ReturnOrigins`) | Old code trusted client `origin_url` (open-redirect / phishing risk) |
| D-08 | Payment **status endpoint scoped to caller**; **purchase cap enforced at checkout** (409) | Old: any user could poll any session; old `_grant` charged then silently skipped credit after 3 passes |
| D-09 | ffmpeg from **PATH / `Captions:FfmpegPath`** instead of bundled imageio-ffmpeg | No .NET equivalent; documented install |
| D-10 | Probe uses `ffmpeg -i` (no full decode) | Faster uploads; same Duration/Video regexes |
| D-11 | SRT time carries milliseconds correctly | Old could print `,1000` for x.9996 s |
| D-12 | ASS/SRT **byte-identical** to old for all themes/variants | Locked by golden tests - do not "improve" without regenerating fixtures |
| D-13 | Icons: keep **Phosphor** (`@phosphor-icons/react`) inside the studio only | Design guidelines require it, avoids ~40 icon remaps; template pages keep lucide. Only new npm dep |
| D-14 | shadcn/Radix not adopted; 4 tiny primitives in `ui.tsx` | Template has no shadcn; avoids ~25 Radix deps |
| D-15 | Studio CSS **namespaced** (`--ct-*`, `.ct-scope`) and lazy-loaded | Old global resets/`--muted` would break template pages |
| D-16 | Data per **user** (not team) | Old app was single-user; template Teams not needed for captions |
| D-17 | New code isolated in `Caption*`/`features/captions` | Keeps `BASE_SYNC_*` template syncs low-conflict |
| D-18 | AI-SEO module **removed** from backend + Admin API (35 tables, 12 controllers, jobs, services, DTOs, entities); `Projects` FKs from LLM tables dropped, `ProjectId` columns kept | User request 2026-10-22; scripts 22/24 |
| D-19 | **No EF migrations, no startup seeding, no reseed button.** Hand-written SQL in `Database_Migrations/` (22 generic schema, 21 Captain Title + seed data, 23 drift check, 24 AI-SEO removal) is the only way the DB changes; `SchemaTests` compares the EF model with the SQL | User rule (2026-10-22): the user runs all DB changes by hand; also written to `CLAUDE.md` / `.windsurf/rules/postgres.md` |
| D-21 | `RequestResponseLoggingMiddleware` (template) no longer buffers `/api/captions/media/*`, `/download`, `/srt`, and rewinds before copying | It buffered every response in memory and copied from the wrong position: file/binary responses came back empty (500) and videos would have been held in RAM. Found by running the API |
| D-22 | A caption job that cannot start (dependency/config failure) marks the project `failed` instead of leaving it `queued` | Found by running the API without `EncryptionKey` |
| D-23 | Entitlement reads use `AsNoTracking` | Balances change via set-based UPDATEs; a tracked copy returned stale credits within one request (found when granting credits) |
| D-24 | Sync of old `c3dc0e6`: templates are **code-defined for the two fixed designs** (`FixedTemplates.cs`, keyed by theme key); the `CaptionThemes` rows only make them selectable. The 9 old templates are deactivated (not deleted) by migration 26 | Old removed the skinnable-template system. Unknown/flat templates fall back to Street Glow in the export, like the old `THEMES.get(..., street_glow)` |
| D-25 | `CaptionVariants` table and `CaptionProjects.Variant` are kept but **ignored by the export and hidden in the editor** | Old removed variants; keeping the table avoids a destructive change. Drop later if unwanted |
| D-26 | SFX: the five built-in types use the old fixed synthesis (noise/bell/pitch-drop); an admin-defined extra type keeps the previous tone-sweep from its DB params. Unknown type in a saved project -> bass_drop | Old hardcoded the five in code; this preserves admin flexibility |
| D-27 | Doodle Marker word width is measured by `TrueTypeTextMeasurer` (sums glyph advances from the .ttf, whole-pixel rounding) instead of PIL | PIL hinted widths differ by about 1px/glyph; only positions a loose decorative pill/loop. Golden fixtures patch the same measurer; real PIL widths are compared with a tolerance |
| D-28 | Export passes `-map [aout]` only when the source has an audio stream (new `hasAudio`) | The old `filter_complex` rewrite fails on silent videos; the old `-af` form tolerated them |
| D-29 | Render timeout (`Captions:RenderTimeoutSeconds`, default 300) cancels the background render and stores the friendly timeout message | Port of old `RENDER_TIMEOUT_SECONDS`; "high" quality default preset is now medium/CRF 17 (admin `captions.render_*` settings still override) |
| D-20 | Public `GET /api/captions/catalog` is anonymous | Contains only presentation data and limits |

## Old-app issues observed (not silently fixed unless noted)

1. Credit lost after 3 single passes (fixed: D-08).
2. Payment status readable by any user (fixed: D-08).
3. Subscription: credits granted once; no renewal/cancel handling (backlog P1).
4. Open redirect via `origin_url` (fixed: D-07).
5. `render_path`/`preview_path` leaked in project JSON (now omitted).
6. CORS `allow_origin_regex=https?://.*` with credentials (template uses explicit allow-list).
7. Debug artefacts (cookies, 90 MB source videos) committed in the old repo root (not carried over).
8. Playfair/DejaVu font mismatches (see CUSTOM_CODE_REGISTER §4).
