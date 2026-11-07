# Deployment notes (ffmpeg, payments, rebrand)

## ffmpeg
Resolution order: `Captions:FfmpegPath` -> bundled `Tools/ffmpeg/<rid>/` -> `PATH`.
The build needs **libx264** and **libass** (subtitle burn-in). Check at `GET /api/admin/captions/health` (or the Captain Title admin page).
- Docker/Linux VM (recommended): `apt-get install -y ffmpeg` (includes both).
- macOS dev: `brew install ffmpeg-full` (plain `ffmpeg` has no libass).
- Shared Windows hosting (e.g. MonsterASP/runasp.net): nothing can be installed, so bundle it. Put a Windows build with libx264 and libass at `AppBackendBasic.API/Tools/ffmpeg/win-x64/ffmpeg.exe` (git-ignored; copied by `dotnet publish`), publish, then check the health endpoint. These hosts may still block child processes or limit CPU: if `ffmpeg.problem` says access is denied or renders are killed, use a small VM/container instead.

Cost/scale: draft/high presets are admin settings (`captions.render_*`); defaults use `veryfast` (about 6x less CPU than the old `slow` preset in our 60s benchmark). Rendering is a background queue (`Captions:MaxConcurrentJobs`, `FfmpegThreads`); the DB claim makes multiple instances safe. Client-side ffmpeg.wasm was evaluated and not built (slow, memory-bound on phones).

## Payments
- India (INR): Razorpay. Set `Razorpay:KeyId`, `KeySecret`, `WebhookSecret`. Webhook URL `/api/captions/payments/razorpay/webhook`, events `order.paid`, `payment.captured`, `subscription.charged`. Subscriptions need `RazorpayPlanId` on the plan (admin).
- Other currencies: Stripe. `Stripe:SecretKey`, `WebhookSecret`; webhook `/api/captions/payments/stripe/webhook`.
- Renewals/cancellation handling is deferred.

## Database
Manual SQL only, see `Database_Migrations/RUN_ORDER.md` (order 23 -> 25; fresh 22 -> 21 -> 25).

## Stored videos (saved projects)

Users come back to their projects, so the files under `Captions:StorageRoot` (default `App_Data/caption_storage`) are now long-lived user data, not a cache.

- **Publishing must not delete them.** On MonsterASP/Web Deploy, keep "Remove additional files at destination" off, or set `Captions:StorageRoot` to a folder outside the site root. If the files disappear the editor still opens (edits are in the database) but shows "This video couldn't be loaded".
- **Back them up with the database.** A database backup alone doesn't include the videos.
- **Disk grows with every upload** until the user deletes the project: there is no automatic expiry. Watch the hosting quota. Each project keeps its source, an optional preview, one export (older exports are removed) and a thumbnail.
- **One server only** with `LocalCaptionStorage`. Implement `ICaptionStorage` on S3/R2 before running more than one API instance.
