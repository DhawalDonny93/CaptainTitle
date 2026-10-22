---
trigger: always_on
---
## PostgreSQL rule: the user runs all database changes by hand

Applies to this project and to ANY project that uses a PostgreSQL database (Supabase included).

1. **Every database change is delivered as a PostgreSQL `.sql` script that the USER executes manually** (for example in the
   Supabase SQL editor). This covers new/changed/dropped tables, columns, indexes, constraints, functions, and seed/reference data.
2. **Never create or run anything that changes the database for the user.** That means: no EF Core / Flyway / Alembic / Knex /
   Prisma migrations, no `dotnet ef database update`, no migration runner or `migrate.sh`-style scripts, no code that creates tables or
   inserts seed data at application startup, no "reseed"/"auto-migrate" admin buttons, and no executing scripts against the user's
   database. Do not add `Database.Migrate()` / `EnsureCreated()`.
3. Write the script so it is safe to run by hand: a header (purpose, prerequisites, run order, rollback notes), `IF NOT EXISTS` /
   `ON CONFLICT DO NOTHING` where possible, one transaction (`BEGIN; ... COMMIT;`), destructive steps clearly labelled with a backup
   warning, and a read-only check query when the current database state is unknown.
4. Save the script in the project's SQL folder (here: `Database_Migrations/`, next free number) AND present it to the user in the reply
   with the order to run it in. When several scripts exist, state the run order explicitly.
5. Keep code and SQL in sync: whenever an entity/model changes, update the SQL script in the same change. A test that compares the
   ORM model with the SQL files is encouraged.
6. Testing a script on a throw-away local Postgres (never the user's database) is allowed and encouraged; say what was and was not tested.

