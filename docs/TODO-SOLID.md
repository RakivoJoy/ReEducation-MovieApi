# TODO-SOLID

Backlog for making MovieApi easier to change while preserving its HTTP contracts and database behavior. SOLID-01 through SOLID-03 are complete. Revisit SOLID-04 when movie reads gain another caller or substantial query logic.

## ICE scoring

Scores follow the [ICE backlog template](templates/ICE-BACKLOG.md).

| ID | Backlog item | Principle | Impact | Confidence | Ease | ICE | Status |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| SOLID-01 | Extract the cast assignment workflow | SRP | 9 | 9 | 6 | 486 | Done (`1bced52`) |
| SOLID-02 | Give movie cast entries their own response DTO | SRP | 5 | 9 | 9 | 405 | Done (`d1abd46`) |
| SOLID-03 | Separate demo data from development database initialization | SRP | 6 | 9 | 7 | 378 | Done (`311c6df`) |
| SOLID-04 | Move movie read projections into a focused query class | SRP | 5 | 8 | 7 | 280 | Later |

SRP is the Single Responsibility Principle. The completed items were verified with 42 Release tests and warnings treated as errors.

### SOLID-01 Extract the cast assignment workflow

`CastAssignmentService` checks resources, writes the link or legacy role, and handles database constraint errors. `PostMovieActor` maps the service outcome to an HTTP response. [CastAssignmentService.cs](../src/MovieApi/Services/CastAssignmentService.cs) · [MoviesController.cs](../src/MovieApi/Controllers/MoviesController.cs)

**Acceptance criteria**

- [x] The service contains no MVC result types, and the action delegates database error handling to the service.
- [x] Integration tests cover `201` for a new link, `204` when filling a legacy role, `404` when either resource is missing, and `409` when a role already exists.
- [x] The database still prevents duplicate links, and the conditional update prevents a concurrent request from overwriting an assigned role.

### SOLID-02 Give movie cast entries their own response DTO

`MovieDetailDto.Actors` uses `MovieCastMemberDto` for film-specific roles. Actor endpoints return `ActorDto`. [ActorDto.cs](../src/MovieApi/DTOs/ActorDto.cs) · [MovieCastMemberDto.cs](../src/MovieApi/DTOs/MovieCastMemberDto.cs)

**Acceptance criteria**

- [x] The actor endpoints still return `id`, `name`, and `birthYear`, and their OpenAPI schema omits `role`.
- [x] Both movie detail routes still return the same cast JSON. A saved role is included and an unassigned role is omitted.

### SOLID-03 Separate demo data from development database initialization

`DemoMovieCatalog` constructs the sample movies and their related data. `SeedDataExtensions` handles startup checks, migrations, and the transaction around the empty-catalog check and inserts. [DemoMovieCatalog.cs](../src/MovieApi/Data/DemoMovieCatalog.cs) · [SeedDataExtensions.cs](../src/MovieApi/Extensions/SeedDataExtensions.cs)

**Acceptance criteria**

- [x] Demo catalog construction can run without a web host or database.
- [x] Integration tests verify that development initialization seeds an empty catalog and preserves an existing one.
- [x] Environment and configuration gates, migration order, and transaction behavior are preserved.

### SOLID-04 Move movie read projections into a focused query class

Revisit when movie reads gain another caller or substantial query logic. Put catalog filtering and detail projection in a concrete `MovieQueries` class that uses EF Core. Keep HTTP responses in the controller and preserve SQL-side filtering and LINQ `Select` projections. [MoviesController.cs](../src/MovieApi/Controllers/MoviesController.cs)

**Acceptance criteria**

- [ ] The list endpoint still combines genre, year, and actor filters and returns summaries.
- [ ] Both film detail routes still use the same projection and return `404` for a missing movie.
- [ ] Existing route and response integration tests pass, including the POST response's `Location` link.
