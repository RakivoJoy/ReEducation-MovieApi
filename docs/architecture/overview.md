# MovieApi architecture

ASP.NET Core routes HTTP requests to the movie, actor, and review controllers. Controllers use request DTOs for input and project database results into response DTOs. The movie controller delegates cast-role assignment to `CastAssignmentService`, then maps the outcome to an HTTP response.

The controllers and cast assignment service use a scoped `MovieDbContext`. EF Core maps the domain entities to SQL Server and translates their queries and updates. For example, the database stores a role on the `MovieActor` join row, and the API exposes it in a movie's cast response.

## Diagrams

- [Application UML](application-uml.md) shows the main types and their dependencies.
- [SQL Server entity-relationship diagram](database-erd.md) shows the tables, keys, and cardinalities.
- [Cast assignment sequence](cast-assignment-sequence.md) shows the request flow and possible HTTP outcomes.
