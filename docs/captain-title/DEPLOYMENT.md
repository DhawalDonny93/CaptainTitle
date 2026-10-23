# Deployment notes (ffmpeg, payments, rebrand)

## ffmpeg
Resolution order: `Captions:FfmpegPath` -> bundled `Tools/ffmpeg/<rid>/` -> `PATH`.
The build needs **libx264** and **libass** (subtitle burn-in). Check at `GET /api/admin/captions/health` (or the Captain Title admin page).
- Docker/Linux VM (recommended): `apt-get install -y ffmpeg` (includes both).
- macOS dev: `brew install ffmpeg-full` (plain `ffmpeg` has no libass).
- Shared Windows hosting (e.g. runasp.net) usually cannot run child processes: use a small VM/container instead.

Cost/scale: draft/high presets are admin settings (`captions.render_*`); defaults use `veryfast` (about 6x less CPU than the old `slow` preset in our 60s benchmark). Rendering is a background queue (`Captions:MaxConcurrentJobs`, `FfmpegThreads`); the DB claim makes multiple instances safe. Client-side ffmpeg.wasm was evaluated and not built (slow, memory-bound on phones).

## Payments
- India (INR): Razorpay. Set `Razorpay:KeyId`, `KeySecret`, `WebhookSecret`. Webhook URL `/api/captions/payments/razorpay/webhook`, events `order.paid`, `payment.captured`, `subscription.charged`. Subscriptions need `RazorpayPlanId` on the plan (admin).
- Other currencies: Stripe. `Stripe:SecretKey`, `WebhookSecret`; webhook `/api/captions/payments/stripe/webhook`.
- Renewals/cancellation handling is deferred.

## Database
Manual SQL only, see `Database_Migrations/RUN_ORDER.md` (order 23 -> 25; fresh 22 -> 21 -> 25).
