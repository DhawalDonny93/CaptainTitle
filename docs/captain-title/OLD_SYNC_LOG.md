# Keeping up with the old project

The old repo will keep changing. Everything needed to import those changes (and deletions) safely is in this repo.

## Workflow

1. `scripts/old-sync/check-old-changes.sh /Users/dhawal/Documents/GitHub/CaptainTitle_Old/captain-title`
   Lists every **ADDED / MODIFIED / DELETED** tracked file since the recorded baseline, each mapped (via
   `docs/captain-title/old-baseline/port-map.tsv`) to the new file(s) that must be updated and the port status. `UNMAPPED` = new area: add a row.
2. For each change, edit the mapped new file(s). Backend logic changes in `caption_ai.py` / `captions.py`:
   re-run `scripts/old-sync/generate_golden.py` -> the golden tests fail where behaviour changed -> update the C# until green (or accept an intentional deviation and document it in DECISIONS.md).
3. Old **deletions**: remove/adjust the port, then record it below under "Deleted in old".
4. Record the sync in the ledger, then `check-old-changes.sh ... --accept` to move the baseline (updates `files.manifest` + `BASELINE_COMMIT`).
5. Ask Claude: "sync old project" - the script output is the work list.

## Hard-won rules
- Hardcoded value changed in old code? Update the seed rows in `Database_Migrations/21_captain_title_captions.sql` (and `CaptionSeedData`, used by tests) **and** hand admins an `UPDATE` script (existing DBs keep their edited rows - `ON CONFLICT DO NOTHING`). Golden `Seed_tables_match_old_hardcoded_tables` + `CaptionSeedSqlTests` flag drift.
- New old-frontend component? Add it under `features/captions/components` and a row to the port map.
- New old API route? Add to `CaptionsController`, DTOs in `CaptionDtos.cs` (snake_case), TS in `captionsApi.ts`.
- Any DB change -> hand-written SQL in `Database_Migrations/` (never a migration/auto-seed - see CLAUDE.md); `SchemaTests` checks the EF model against the SQL.

## Ledger

| Date | Old commit | Direction | Summary | Done by |
|---|---|---|---|---|
| 2026-10-21 | `4ee2eea` | Baseline | Initial full port (185 tracked files, see port-map) | Claude |

### Deleted in old (after baseline)
_none yet_

### New in old, intentionally not ported
_none yet_

### Old-side files intentionally NOT carried over at baseline
Emergent scaffolding (`.emergent/`, `frontend/plugins/`, `craco.config.js`, PostHog/emergent scripts in `public/index.html`), shadcn components the editor never imported (≈45 files in `components/ui/`), test IDs registry, debug media/JSON/cookie artefacts at repo root, 90 MB `flying-beast-source.mp4`.
