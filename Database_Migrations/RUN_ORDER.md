# Database scripts - what to run, and in which order

**The application never creates, migrates or seeds the database.** You run these `.sql` files yourself (Supabase ▸ SQL editor).
They were tested on a local PostgreSQL 18 (fresh database, re-run, and an upgrade of the pre-change schema with data) - not on your Supabase project.

| # | File | What it does | Destructive? |
|---|---|---|---|
| 23 | `23_schema_drift_check.sql` | Read-only report: missing tables/columns, leftover AI-SEO tables (with row counts), type of `"User"."Id"` | No |
| 22 | `22_generic_template_schema.sql` | Creates the 50 generic template tables + indexes (users, auth, sessions, subscriptions/quotas, notifications, onboarding, compliance, LLM, admin, teams). `IF NOT EXISTS`: only adds what is missing | No |
| 21 | `21_captain_title_captions.sql` | Creates the 10 Captain Title tables and loads the factory catalog, `captions.*` settings and the 2 LLM job rows (`ON CONFLICT DO NOTHING`: your edits survive re-runs) | No |
| 24 | `24_remove_ai_seo_tables.sql` | Drops the 35 old AI-SEO tables (CASCADE) and removes their Hangfire schedules/jobs | **Yes - backup first** |

## Fresh (empty) database
`22` → `21`. (24 is not needed.)

## Existing database that ran the previous template code
1. Backup.
2. `23` - read the result sets. Expect: 10 MISSING TABLE (Captain Title), 35 LEFTOVER AI-SEO TABLE, no MISSING COLUMN. `"User"."Id"` must be `integer`.
3. `22` (normally a no-op) → `21`.
4. Deploy the new application build.
5. `24` to drop the AI-SEO tables. 
6. `23` again - everything empty.

"relation ... already exists, skipping" notices are normal on re-runs (that is the idempotency).

## Notes
- `"ProjectId"` on `"LlmRequest"`, `"LlmCostTracking"`, `"QuotaUsageLogs"` stays as a plain nullable integer (history kept); only its foreign key to the dropped `"Projects"` goes.
- `SubscriptionPlans` keeps its legacy quota columns (`MaxProjects`, `MaxPromptsPerProject`, `MaxVisibilityChecksPerMonth`, `MaxSiteAuditsPerMonth`, `MaxContentArticles`).
- Older scripts in this folder (`01_...` to `20_...`, the root `COMPLETE_*.sql`, `fix_*.sql` ...) are historical and conflict with each other (UUID vs integer ids). Do not run them; `22` replaces them.
- Rollback of 21: the `DROP TABLE` list at the bottom of `21_captain_title_captions.sql`. Rollback of 24 = restore the backup.
