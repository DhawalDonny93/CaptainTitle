# Configuration & running

## Prerequisites
.NET 8 SDK · Node 20+ · PostgreSQL (Supabase OK) · **ffmpeg** with libx264, libvpx-vp9, libopus, libass (`brew install ffmpeg` / `apt install ffmpeg`).

## Old env var -> new setting

| Old (`backend/.env`) | New | Where |
|---|---|---|
| `MONGO_URL`, `DB_NAME` | `ConnectionStrings:DefaultConnection` (Postgres) | appsettings / user-secrets |
| `SARVAM_API_KEY` | `Captions:SarvamApiKey` | secrets |
| `SARVAM_STT_MODEL` | `Captions:SarvamModel` (default `saaras:v4`) | appsettings |
| `OPENAI_API_KEY` | Admin ▸ LLM Providers ▸ OpenAI ▸ ApiKey (or `Captions:OpenAiApiKey` override) | DB / secrets |
| `CAPTION_LLM_MODEL` | Admin ▸ Captain Title ▸ Language models | DB |
| `LOCAL_STORAGE_DIR` | `Captions:StorageRoot` | appsettings |
| `UNLIMITED_TESTING` | setting `captions.unlimited_testing` | Admin |
| `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET` | `Stripe:SecretKey`, `Stripe:WebhookSecret` (or `Captions:StripeWebhookSecret`) | secrets |
| `GOOGLE_CLIENT_ID/SECRET/REDIRECT_URI`, `FRONTEND_URL` | template `GoogleAuth:*` + `VITE_GOOGLE_CLIENT_ID` | see template docs |
| `REACT_APP_BACKEND_URL` | `VITE_API_URL` (includes `/api`) | `.env` |

## Run

```bash
# 1) run the SQL by hand (Supabase SQL editor) - see Database_Migrations/RUN_ORDER.md:  22_generic_template_schema.sql  then  21_captain_title_captions.sql
cd AppBackendBasic.API && cp appsettings.example.json appsettings.Development.json   # fill values
dotnet run
cd ../AppBackendBasic.AdminUI && dotnet run  # admin
cd ../AppBackendBasic.WebUI && npm ci && npm run dev   # http://localhost:3000/studio
```

## Stripe
Create two **Prices** whose *lookup keys* equal the plan rows (`captain_single_45_inr`, `captain_basic_monthly_inr` by default; INR).
Webhook endpoint: `POST {api}/api/captions/payments/stripe/webhook` (event `checkout.session.completed`). Add your site origin to `Captions:ReturnOrigins`.
> The template has a separate Stripe subscription flow for the legacy SEO product (`SubscriptionController`); the two do not share state.

## Tests
```bash
dotnet test AppBackendBasic.Captions.Tests     # golden parity (needs only the committed golden.json) + schema guard
python3 -I scripts/old-sync/generate_golden.py <old>/backend AppBackendBasic.Captions.Tests/golden   # regenerate after old logic changes
```
