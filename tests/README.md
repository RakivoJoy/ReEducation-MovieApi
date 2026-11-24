# Tests

- `MovieApi.UnitTests` covers deterministic logic in isolation.
- `MovieApi.IntegrationTests` exercises HTTP routes and middleware through an in-memory ASP.NET Core host.

Run both xUnit projects from the repository root with `dotnet test MovieApi.slnx`. CI runs them through `scripts/verify.ps1`.
