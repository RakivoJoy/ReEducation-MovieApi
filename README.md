# Movie API

Repository structure for a .NET Movie API.

- `src/` contains application code.
- `tests/` contains unit and integration tests.
- `docs/` holds requirements and architecture decisions.
- `scripts/` contains the verification script used locally and in GitHub Actions.
- `AGENTS.md` contains instructions for Codex.

Add a `.sln` or `.slnx` file at the repository root and include the application and test projects. Then run `pwsh -File scripts/verify.ps1` from the repository root. The script restores dependencies, builds in Release configuration, and runs the tests. If the solution is missing, the script reports that it skipped the checks. CI treats build warnings as errors.
