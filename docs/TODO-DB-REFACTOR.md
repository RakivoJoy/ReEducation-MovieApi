# Database Refactor Backlog

Move MovieApi from SQLite to the existing local SQL Server default instance (`MSSQLSERVER`). Preserve HTTP contracts and database behavior, and keep the setup reproducible from the repository.

## Prioritization

Score Impact, Confidence, and Ease from 1 to 10. ICE is their product. Complete prerequisites before dependent items. Statuses follow the [ICE backlog template](templates/ICE-BACKLOG.md).

| ID | Backlog item | Impact | Confidence | Ease | ICE | Status |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| DB-01 | Set up VS Code and provision SQL Server databases | 10 | 9 | 8 | 720 | Done |
| DB-02 | Switch the EF Core provider and create SQL Server migrations | 10 | 9 | 6 | 540 | Done |
| DB-03 | Port startup seeding and database error handling | 9 | 9 | 5 | 405 | Done |
| DB-04 | Run integration tests against SQL Server | 10 | 9 | 5 | 450 | In progress |
| DB-05 | Assess whether SQLite data needs to be copied | 8 | 8 | 7 | 448 | Done |

## Backlog items

### DB-01 Set up VS Code and provision SQL Server databases

Use Windows Authentication against the existing instance. Save provisioning T-SQL under `scripts/sql/`. Keep development and integration test databases separate.

**Acceptance criteria**

- [x] Confirm the Microsoft SQL Server extension (`ms-mssql.mssql`) is installed.
- [x] Authenticate to the local default instance with Windows Authentication.
- [x] Run the checked-in script twice and verify `MovieApiDev` and `MovieApiIntegrationTests` are online after both runs.

**Completion**

Installed the Microsoft SQL Server extension (v1.48.0) and verified the [database provisioning script](../scripts/sql/create-movieapi-databases.sql) with two successful runs. Both databases reported `ONLINE` each time.

From the repository root, run this against the local Windows instance using shared memory and Windows Authentication:

```powershell
sqlcmd -S lpc:localhost -E -C -b -i scripts/sql/create-movieapi-databases.sql
```

### DB-02 Switch the EF Core provider and create SQL Server migrations

After DB-01, configure the SQL Server provider at the project's EF Core version. Let EF migrations own application tables and constraints.

**Acceptance criteria**

- [x] Apply SQL Server migrations to a fresh database and verify the expected tables, keys, indexes, and constraints.
- [x] Document local connection configuration and migration commands in the README. Keep credentials out of source control.

**Completion**

Applied the [generated schema](../scripts/sql/movieapi-schema.sql) to `MovieApiDev` and inspected its tables, keys, indexes, and rating constraint. EF Core reports no pending model changes. The [README](../README.md) documents the Windows Authentication configuration and migration commands.

### DB-03 Port startup seeding and database error handling

After DB-02, update empty-catalog seeding and the SQLite-specific exception handling in cast assignment and review creation.

**Acceptance criteria**

- [x] Verify startup seeds an empty catalog, preserves existing data, and avoids duplicate inserts during concurrent initialization.
- [x] Verify duplicate cast assignment and foreign-key failures preserve the corresponding HTTP behavior.

**Completion**

[Startup tests](../tests/MovieApi.IntegrationTests/SeedDataTests.cs) start two hosts against the same provisioned database. [API tests](../tests/MovieApi.IntegrationTests/MoviesApiTests.cs) force actual SQL Server constraint failures by inserting a competing cast link or deleting the movie before the review insert. The API returns 409 and 404 respectively.

### DB-04 Run integration tests against SQL Server

Coordinate with DB-02 and DB-03. Exercise the actual provider's constraints, migrations, and transaction behavior. Isolate tests from development data and from concurrent test runs.

**Acceptance criteria**

- [x] Database and API integration tests run against SQL Server and cover the existing SQLite-backed scenarios.
- [x] The full solution test suite passes locally.
- [ ] Confirm the GitHub Actions run starts SQL Server and passes the suite.

**Verification**

All 44 tests passed locally in Release. Each database test uses an isolated SQL Server database. The [verification workflow](../.github/workflows/verify.yml) provisions a disposable SQL Server container and waits for a successful query before running the suite. Hosted verification is pending.

### DB-05 Assess whether SQLite data needs to be copied

Resolve before retiring the SQLite workflow. Demo-only data can be recreated through seeding.

**Acceptance criteria**

- [x] Determine whether any existing records must be kept and record the decision.

**Completion**

Opened the local `src/MovieApi/movieapi.db` read-only and compared its two movies, two details records, three reviews, three actors, and four cast links with the demo catalog. All values and relationships matched. The SQL Server development API reproduced the demo catalog on startup, so this local database requires no data transfer. The original SQLite file is still available.
