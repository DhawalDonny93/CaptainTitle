# Custom code & hardcoded values register

Everything below is something a future maintainer might need to change. **DB** = already admin-editable. **CODE** = hardcoded.

## 1. Moved out of code into the database (was hardcoded in the old app)

| Old hardcode | Old location | Now |
|---|---|---|
| THEMES (9 templates, tiers) | `captions.py` THEMES + `ThemePanel.jsx` templates | DB `CaptionThemes` (seed: `CaptionSeedData.Themes`) |
| FONTS | `captions.py` FONTS + `ThemePanel.jsx` fonts | DB `CaptionFonts` |
| VARIANTS | `captions.py` VARIANTS + `ThemePanel.jsx` variants | DB `CaptionVariants` |
| SFX tone params (2 copies!) | `captions.py` SFX_TONE_PARAMS + `sfx.js` | DB `CaptionSfxTypes` (single source) |
| STICKERS | `ElementsPanel.jsx` | DB `CaptionStickers` |
| FILLER_WORDS | `AiToolsPanel.jsx` | DB `CaptionFillerWords` |
| CATALOG / prices ₹69, ₹399, lookup keys, "3 purchases" cap, 45/60 s, 1/10 videos | `payments.py`, `captions.py _entitlement`, `PaywallDialog.jsx`, `UploadStage.jsx` | DB `CaptionPlans` |
| 120 MB, 120 s, 45 s free, 1 free video, `.mp4/.mov`, UNLIMITED_TESTING env | `captions.py`, `UploadStage.jsx`, env | SystemSettings `captions.*` |
| LLM model `gpt-4o` (`CAPTION_LLM_MODEL`) | `caption_ai.py` | `LlmJobConfigs` rows |
| Default template "viral" | `UploadStage.jsx` | setting `captions.default_theme` |

## 2. Still hardcoded in code (CODE)

### Backend
| Value | File | Why it is code |
|---|---|---|
| Export canvas 1080x1920 (`PlayResX/Y`) | `SubtitleWriters.cs` | Design constant; the editor preview scales against 1080 |
| Position presets top(`\an8`,150) / middle(`\an5`,0) / bottom(`\an2`,210) | `CaptionLayout.PositionPreset` | Layout rule |
| Caption grouping: min display 0.35 s, max 3 words (Auto), max gap 0.4 s, sentence-end chars `.!?।॥` | `CaptionLayout.cs` **and** `CaptionStudio.tsx` | Mirrored; change both |
| Emphasize scale ×1.18, default accent `#EAB308` | `SubtitleWriters.cs`, `studio.css` `.emphasize-accent` | Visual rule |
| Chunking: 20 s target, ±2 s snap, 1.5 s overlap, 0.5 s lead-in; silence −35 dB / 0.3 s | `CaptionTranscriptionService.cs`, `FfmpegService.cs` | Tuned for Sarvam's 30 s cap (23.5 s worst case) |
| Trust cutoffs: no-speech ≥ 0.65, avg logprob > −1.45, unique < .24, bigram > .30, 4-gram ≥ 3 | `TranscriptionAlgorithms.cs` | Hallucination guards from old iterations 3-4 |
| Alignment gap −0.6, match offset −0.4 | `TranscriptionAlgorithms.AlignWords` | Algorithm constants (golden-tested) |
| Whisper request: `whisper-1`, temperature 0, word granularity only; Sarvam `saaras:v4`, mode `codemix`, `hi-IN`/`unknown` | `SpeechClients.cs`, `CaptionOptions` | Models are config; request shape is code |
| Hinglish + emphasis system prompts | `HinglishService.cs` | Prompt engineering - treat as code, golden prompts copied verbatim |
| Preview encode: 720 px, VP9 realtime, 1100 k, Opus 96 k | `FfmpegService.CreateBrowserPreviewAsync` | |
| Render presets: high = slow/CRF 17, draft = veryfast/CRF 26; AAC 192 k; SFX volume 0.7; amix normalize=0 | `FfmpegRenderArgs.cs` | |
| Word limits: text 1-80 chars, font 24-92, element 20-280, volume 0-200 … | `CaptionDtos.cs` | Validation mirrors old pydantic models |
| Allowed positions `top/middle/bottom`; speech languages `hi/auto`; qualities `high/draft` | `CaptionProjectService.cs`, DTOs | |
| Storage path layout `captain-title/{uploads,previews,renders}/{userId}/{guid}` | `CaptionProjectService`, `CaptionJobRunner` | |
| Media cache `private, max-age=3600` | `CaptionsController.Media` | |
| Job messages ("Preparing audio" 12 %, "Listening word by word" 28 %, "Creating Roman Hinglish" 78 %) | `CaptionJobRunner.cs` | UI shows them verbatim |
| Hinglish LLM defaults `OpenAI/gpt-4o/16000/0` (first-run seed only) | `CaptionSeedData.LlmJobs` | After seeding edit in Admin |

### Frontend
| Value | File |
|---|---|
| Grouping constants (see above), `POSITION_DEFAULTS` (top 14 %, middle 50 %, bottom 86 %), resize floors 60 × 30 px | `CaptionStudio.tsx` |
| Poll interval 2.5 s (upload & re-listen), payment poll 2 s | `UploadStage.tsx`, `CaptionStudio.tsx`, `PaymentResult.tsx` |
| Words-per-caption options `Auto,1,2,3,4,5,6,8`; lines 1/2; positions | `ThemePanel.tsx` |
| Timeline zoom steps, base 60 px/s, waveform 20 peaks/s, canvas cap 8000 px | `Timeline.tsx` |
| Kinetic styles list (pulse/pop/scale/box/wobble) + their CSS keyframes | `MomentPopover.tsx`, `studio.css` |
| Brand name, colours, copy, fonts import | `EditorHeader.tsx`, `studio.css` |
| Studio routes `/studio`, `/payment/success`, `/payment/cancel` (the latter two are sent to Stripe) | `App.tsx`, `CaptionBillingService` |

### Template-level hardcodes that affect Captain Title (CODE, in the template)
| Item | File | Action |
|---|---|---|
| CORS allow-list contains `auraaiseo.netlify.app`, `basicframeworkwebui.netlify.app`, localhost ports | `AppBackendBasic.API/Program.cs` | Replace with your domains (or move to config) |
| DataProtection `SetApplicationName("BasicFrameworkBackend")` | `Program.cs` | Rename (changing it invalidates existing protected data/tokens: do it before launch) |
| Swagger title "AppBackendBasic API" | `Program.cs` | Rename |
| Kestrel/request limit: upload endpoint uses `[DisableRequestSizeLimit]`; real cap = `captions.max_upload_mb` | `CaptionsController.Process` | Put a reverse-proxy limit too (nginx `client_max_body_size`) |

## 3. Custom (non-obvious) code pieces

| Piece | Why it exists |
|---|---|
| `DifflibRatio.cs` | Exact port of Python `difflib.SequenceMatcher.ratio()` - alignment results depend on it |
| `WavAudio.cs` | Replaces Python's `wave` module (chunk slicing/priming) |
| `CaptionMediaTokenService` | `<video>` cannot send `Authorization`; signed 2 h URL replaces the old cookie |
| `CaptionJobWorker` | In-process queue + startup resume (replaces asyncio tasks / `resume_pending_projects`) - single-node only |
| `CaptionStyleFactory.ParseTiers` | Validates tier JSON (must contain `support`) |
| `studio.css` scoping (`--ct-*`, `.ct-scope`) | Keeps the dark studio from leaking into the template's light pages |
| `ui.tsx` (CtSlider etc.) | Minimal replacements for shadcn/Radix controls with the same props contract |
| `scripts/old-sync/generate_golden.py` | Re-generates parity fixtures from the old Python (stubs fastapi/openai/sarvamai) |

## 4. Inherited quirks (kept for parity, flagged)

- Tiered themes reference ASS font **"Playfair Display SemiBold"** but only `PlayfairDisplay-Italic.ttf` is bundled: libass falls back to another font in exports. Fix by adding a matching file or changing the tier font.
- `classic` font = "DejaVu Sans" is **not bundled** (relies on the render server's system font).
- Kinetic animation, box width/height are editor-only (not in the exported video), as in the old app.
