# Database Refactor Backlog

Move MovieApi from SQLite to the existing local SQL Server default instance (`MSSQLSERVER`). Preserve HTTP contracts and database behavior, and keep the setup reproducible from the repository.

## Prioritization

Score Impact, Confidence, and Ease from 1 to 10. ICE is their product. Complete prerequisites before dependent items. Statuses follow the [ICE backlog template](templates/ICE-BACKLOG.md).

| ID | Backlog item | Impact | Confidence | Ease | ICE | Status |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| DB-01 | Set up VS Code and provision SQL Server databases | 10 | 9 | 8 | 720 | In progress |
| DB-02 | Switch the EF Core provider and create SQL Server migrations | 10 | 9 | 6 | 540 | Ready |
| DB-03 | Port startup seeding and database error handling | 9 | 9 | 5 | 405 | Ready |
| DB-04 | Run integration tests against SQL Server | 10 | 9 | 5 | 450 | Ready |
| DB-05 | Assess whether SQLite data needs to be copied | 8 | 8 | 7 | 448 | Ready |

## Backlog items

### DB-01 Set up VS Code and provision SQL Server databases

Use Windows Authentication against the existing instance. Save provisioning T-SQL under `scripts/sql/`. Keep development and integration test databases separate.

**Acceptance criteria**

- [ ] Confirm the Microsoft SQL Server extension (`ms-mssql.mssql`) is installed.
- [ ] Authenticate to the local default instance with Windows Authentication.
- [ ] Run the checked-in script twice and verify `MovieApiDev` and `MovieApiIntegrationTests` are online after both runs.

### DB-02 Switch the EF Core provider and create SQL Server migrations

After DB-01, configure the SQL Server provider at the project's EF Core version. Let EF migrations own application tables and constraints.

**Acceptance criteria**

- [ ] Apply SQL Server migrations to a fresh database and verify the expected tables, keys, indexes, and constraints.
- [ ] Document local connection configuration and migration commands in the README. Keep credentials out of source control.

### DB-03 Port startup seeding and database error handling

After DB-02, update empty-catalog seeding and the SQLite-specific exception handling in cast assignment and review creation.

**Acceptance criteria**

- [ ] Verify startup seeds an empty catalog, preserves existing data, and avoids duplicate inserts during concurrent initialization.
- [ ] Verify duplicate cast assignment and foreign-key failures preserve the corresponding HTTP behavior.

### DB-04 Run integration tests against SQL Server

Coordinate with DB-02 and DB-03. Exercise the actual provider's constraints, migrations, and transaction behavior. Isolate tests from development data and from concurrent test runs.

**Acceptance criteria**

- [ ] Database and API integration tests run against SQL Server and cover the existing SQLite-backed scenarios.
- [ ] The full solution test suite passes locally and CI has a repeatable SQL Server setup.

### DB-05 Assess whether SQLite data needs to be copied

Resolve before retiring the SQLite workflow. Demo-only data can be recreated through seeding.

**Acceptance criteria**

- [ ] Determine whether any existing records must be kept and record the decision.
- [ ] If records must be kept, save the transfer scripts under `scripts/sql/` and verify row counts and relationships against a copy before cutover.
