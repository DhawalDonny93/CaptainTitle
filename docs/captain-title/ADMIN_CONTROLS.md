# Admin controls - what to change, and where

Sign in to the AdminUI (Razor app) as an Admin and open **Captain Title** in the sidebar (`/Captions`). The factory defaults are loaded by `Database_Migrations/21_captain_title_captions.sql` (run manually); after that the **database is the source of truth** (changes apply within ~30 s, the catalog is cached).

## A. In the Admin UI (no deploy needed)

| I want to change... | Page | Notes |
|---|---|---|
| Caption templates (colours, accent font, tiered support/feature/key styles, preview card) | Captain Title ▸ Caption templates | Add/deactivate templates. Tiered = fill *Tiers JSON*. Deleting a template used by projects is blocked: deactivate. |
| Selectable caption fonts | ▸ Caption fonts | `Font family` must exist as a font file in `AppBackendBasic.API/Assets/captions/fonts` (libass resolves by family name) - **adding a family also needs the .ttf deployed**. |
| Style variants (Rock/Glow/Swiss/Clean) | ▸ Style variants | Outline width/colour, shadow, blur, emphasis accent, CSS preview strings. |
| SFX tones | ▸ Sound effects | Frequency sweep + duration. The same numbers drive the browser preview and the ffmpeg export. |
| Sticker glyphs | ▸ Stickers | Glyph must render with the server's fallback font or it shows as a box in the export. |
| Filler-word list | ▸ Filler words | Used by AI tools "Remove all". |
| Prices, passes, subscription size, per-video length, purchase cap, Stripe lookup key | ▸ Plans & prices | `Kind=payment` -> one-off credits; `subscription` -> monthly credits (granted on purchase). The Stripe **Price must carry the same lookup key**. |
| Free videos per new account, free max length, absolute max length, max upload MB, allowed extensions, default template, **unlimited testing** | ▸ Limits & switches | Stored in SystemSettings, category `captions`. |
| Which LLM/model converts Hindi->Hinglish and tags emphasis; max tokens; temperature | Captain Title ▸ Language models (job keys `CaptionHinglishConversion`, `CaptionEmphasisAssignment`) | Seeded as OpenAI `gpt-4o`, temp 0, 16000 tokens (= old behaviour). |
| OpenAI API key (used for Whisper timing **and** Hinglish when provider=OpenAI) | LLM Providers | Whisper always uses OpenAI. |
| Re-queue a failed project; look at statuses/errors | ▸ Projects & credits | Re-queue does not charge a credit again. |
| Give/remove credits for a user | ▸ Projects & credits ▸ User credits | Support tool; logged. |

## B. In configuration (deploy / secrets) - see [CONFIGURATION.md](CONFIGURATION.md)

`Captions:SarvamApiKey`, `Captions:FfmpegPath`, `Captions:StorageRoot`, `Captions:FontsDir`, `Captions:MaxConcurrentJobs`,
`Captions:MediaTokenMinutes`, `Captions:ReturnOrigins` (allowed Stripe return origins), `Stripe:SecretKey`, `Stripe:WebhookSecret`,
`Captions:OpenAiApiKey` (optional override), `Captions:WhisperModel`, `Captions:SarvamModel`.

## C. Needs a code change

See [CUSTOM_CODE_REGISTER.md](CUSTOM_CODE_REGISTER.md) section "Hardcoded in code". Highlights: brand name/CORS origins/DataProtection app
name in `Program.cs`; pipeline thresholds (chunk size, silence, trust cutoffs); export canvas 1080x1920; caption grouping rules (mirrored in
TS and C#); Hinglish prompts; emphasis scale 1.18.

## D. "System project" style settings

The template's own `SystemSettings`, feature flags, LLM job configs, email templates and onboarding admin remain available in the
existing AdminUI pages; Captain Title only adds the `captions.*` keys. If by "System Projects" you meant a different list of items,
tell me which and I will map it.
