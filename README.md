# Movie API

Repository structure for a .NET Movie API.

- `src/` contains application code.
- `tests/` contains unit and integration tests.
- `docs/` holds requirements and architecture decisions.
- `scripts/` contains the verification script used locally and in GitHub Actions.
- `AGENTS.md` contains instructions for Codex.

Run `pwsh -File scripts/verify.ps1` from the repository root to restore dependencies, build in Release configuration, and run the tests. CI treats build warnings as errors.

## Local database

Run these commands from the repository root to create or update the SQLite development database:

```powershell
dotnet tool restore
Set-Location src/MovieApi
dotnet ef database update
```

The commands apply the migrations in order and create `src/MovieApi/movieapi.db`. Each movie can have one details record, multiple reviews, and multiple actors. EF Core manages the `MovieActor` join table through the movie and actor navigation properties.

Starting the API with the `http` or `https` launch profile applies pending migrations and fills an empty movie catalog with demo films, details, reviews, and actors. An existing movie catalog is left intact on startup. To skip startup initialization for a local profile, set its `SeedData__Enabled` environment variable to `false`.
