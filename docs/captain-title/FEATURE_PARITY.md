# Feature parity matrix (old app -> new)

Legend: ✅ same behaviour · ➕ same + improvement · 🔁 intentionally replaced · ⏳ pending

| # | Old feature (source) | New location | Status |
|---|---|---|---|
| 1 | Email/password register+login, 7-day cookie sessions (`auth.py`) | Template JWT auth + refresh tokens | 🔁 |
| 2 | Google OAuth redirect login (`auth.py`) | Template Google sign-in (ID-token flow) | 🔁 |
| 3 | `GET /auth/me`, logout | Template `/api/auth/*` | 🔁 |
| 4 | Upload MP4/MOV, size cap 120 MB, duration cap 120 s, probe (`captions.py process_video`) | `CaptionsController.Process` -> `CaptionProjectService.CreateFromUploadAsync` (limits are settings) | ➕ |
| 5 | Entitlement ladder testing->free(<=45 s)->one-off(<=45 s)->Basic(<=60 s) | `CaptionEntitlementService` (all numbers from settings/plans) | ➕ |
| 6 | Background job + progress labels (12/28/78/100 %) + startup resume | `CaptionJobWorker` + `CaptionJobRunner` | ✅ |
| 7 | Audio extraction 16 kHz mono PCM | `FfmpegService.ExtractAudioAsync` | ✅ |
| 8 | 720p VP9/Opus browser preview built in parallel | `FfmpegService.CreateBrowserPreviewAsync` | ✅ |
| 9 | Adaptive ~20 s chunking snapped to silence, 0.5 s lead-in, 1.5 s overlap priming | `CaptionTranscriptionService.SplitWavAsync` + `TranscriptionAlgorithms` | ✅ golden |
| 10 | Whisper word timing + Sarvam text, Needleman-Wunsch stitch, interpolation | `TranscriptionAlgorithms.AlignWords/StitchChunk`, `WhisperClient`, `SarvamClient` | ✅ golden |
| 11 | Repetition/hallucination rejection, "no trustworthy transcript" error | `TranscriptionAlgorithms.LooksRepetitive`, `NoTrustworthyTranscriptException` | ✅ golden |
| 12 | Hinglish romanisation + 3-tier emphasis + uncertain flags (GPT) | `HinglishService` (job key `CaptionHinglishConversion`) | ➕ model admin-selectable |
| 13 | Re-listen (re-run without charging) | `POST /retranscribe` | ✅ |
| 14 | Re-run AI emphasis | `POST /ai/emphasis` (job key `CaptionEmphasisAssignment`) | ✅ |
| 15 | 9 templates (6 flat, 3 tiered), 4 fonts, 4 variants | DB tables, seeded 1:1 (golden-checked) | ➕ editable |
| 16 | Save project (words, elements, SFX, grading, layout) | `PUT /projects/{id}` | ✅ |
| 17 | SRT export | `GET /projects/{id}/srt` | ✅ (+ ms carry fix) |
| 18 | Export: ASS burn-in, eq grading before captions, volume/mute, SFX mix, quality presets | `AssSubtitleWriter`, `FfmpegRenderArgs` | ✅ golden |
| 19 | Authenticated media streaming with HTTP Range | `GET /media/{id}?t=` signed URL + ASP.NET range processing | 🔁 (needed for JWT) |
| 20 | Download rendered MP4 | `GET /projects/{id}/download` | ✅ |
| 21 | Stripe checkout (lookup keys), status poll, signed webhook, idempotent grant | `CaptionBillingService` | ➕ hardened (D-07/D-08) |
| 22 | Pricing/paywall dialog, usage meter | `PaywallDialog`, `EditorHeader` (plans from DB) | ✅ |
| 23 | Editor: Media/Audio/Text/Elements/Captions/Adjustment/AI panels | `features/captions/components/*` | ✅ |
| 24 | Live caption overlay: drag, 8-handle resize, "this/all captions" scope, safe-zone guide | `CaptionStudio.tsx` | ✅ |
| 25 | Word timeline: lines/words modes, zoom, waveform, retime handles, SFX markers | `Timeline.tsx` | ✅ |
| 26 | Moment popover: kinetic styles, SFX, emphasize, split/merge, spelling | `MomentPopover.tsx` | ✅ |
| 27 | Script panel with sentence cards + word popover | `TranscriptPanel.tsx` | ✅ |
| 28 | Filler-word cleanup | `AiToolsPanel.tsx` (list from DB) | ➕ |
| 29 | Stickers / free text layers | `ElementsPanel.tsx`, `TextPanel.tsx` | ✅ |
| 30 | Auth screen with brand hero ("Turn every spoken word...") | Template Login (functional) | ⏳ branding (P6) |
| 31 | Dark "tactical" design system (Outfit/IBM Plex/JetBrains Mono, cherry-red palette) | `studio.css` (namespaced) | ✅ editor; ⏳ rest of site |
| 32 | `UNLIMITED_TESTING` env | `captions.unlimited_testing` setting | ➕ |
| 33 | Admin controls | `/Captions` in AdminUI | ➕ new |
| 34 | `GET /api/health` | Template `/health` | 🔁 |
