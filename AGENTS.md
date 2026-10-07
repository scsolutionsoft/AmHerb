# Project instructions

- When a change affects the database schema, create the corresponding EF Core migration and apply it to the project's configured database as part of the same task. Do not leave migration execution as a manual follow-up for the user.
- Verify that no pending migrations remain after applying the change. If execution fails, report the actual blocker and do not claim the database has been updated.
- Use the existing connection configuration without printing credentials. Never reset or drop the database to apply a migration.

This workflow was explicitly requested by the user on 2026-10-07.
