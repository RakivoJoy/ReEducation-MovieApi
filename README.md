# Movie API

Repository structure for a .NET Movie API.

- `src/` contains application code.
- `tests/` contains unit and integration tests.
- `docs/` holds requirements and architecture decisions.
- `scripts/` contains the verification script used locally and in GitHub Actions.
- `AGENTS.md` contains instructions for Codex.

Run `pwsh -File scripts/verify.ps1` from the repository root to restore dependencies, build in Release configuration, and run the tests. CI treats build warnings as errors.

## Documentation

- [DocFX site and C# API reference](https://rakivojoy.github.io/ReEducation-MovieApi/)
- [Architecture overview](docs/architecture/overview.md)
- [Application UML](docs/architecture/application-uml.md)
- [SQL Server entity-relationship diagram](docs/architecture/database-erd.md)
- [Cast assignment sequence diagram](docs/architecture/cast-assignment-sequence.md)

GitHub renders the Mermaid diagrams directly in the linked Markdown files. The DocFX site updates from `main`.

## Local database

On Windows, the Development configuration connects to the local default SQL Server instance using Windows Authentication and the `MovieApiDev` database. From the repository root, provision the database and apply the EF Core migrations:

```powershell
sqlcmd -S lpc:localhost -E -C -b -i scripts/sql/create-movieapi-databases.sql
dotnet tool restore
dotnet ef database update --project src/MovieApi -- --environment Development
```

EF Core migrations define the schema. The [generated SQL script](scripts/sql/movieapi-schema.sql) is available for applying the same migration with `sqlcmd`. Regenerate it after changing migrations with `dotnet ef migrations script --idempotent --project src/MovieApi --output scripts/sql/movieapi-schema.sql -- --environment Development`.

Set `ConnectionStrings__MovieApi` to override the connection for another environment. Keep any credentials in environment variables or another secret store. The checked-in development connection uses the Windows identity and trusts the local server certificate.

Starting the API with the `http` or `https` launch profile applies pending migrations and fills an empty movie catalog with demo films, details, reviews, and actors. An existing movie catalog is left intact on startup. To skip startup initialization for a local profile, set its `SeedData__Enabled` environment variable to `false`.

## Database integration tests

Run `dotnet test MovieApi.slnx`. Database tests create and remove temporary databases named `MovieApiIntegrationTests_<id>` on the local default instance on Windows. For another server, set `MOVIEAPI_TEST_SQLSERVER` to a connection string for a login with permission to create and drop those databases. Use a development or test instance. GitHub Actions starts a disposable SQL Server container for these tests.

GitHub shows separate **Build** and **xUnit tests** checks. Pull requests build automatically. Add the `run-tests` label to run the full xUnit suite before merging. New commits also run the suite while the label is attached. Open the **xUnit tests** check to see whether it ran or was skipped. You can also run tests for a branch from **Actions → Verify → Run workflow**.

Verify checks `main` nightly at 03:17 Stockholm time. It runs the full suite only when the current commit has no successful nightly or manual test run on `main`. A failed test run is retried the next night. Unlabeled PRs still run only the Build check.
