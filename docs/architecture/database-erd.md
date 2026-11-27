# SQL Server entity-relationship diagram

```mermaid
erDiagram
    Movies ||..o| MovieDetails : has
    Movies ||..o{ Reviews : receives
    Movies ||--o{ MovieActor : has
    Actors ||--o{ MovieActor : joins

    Movies {
        int Id PK
    }
    MovieDetails {
        int Id PK
        int MovieId FK, UK
        decimal Budget "19 digits, 4 fractional"
    }
    Reviews {
        int Id PK
        int MovieId FK
        int Rating "1 through 5"
    }
    Actors {
        int Id PK
    }
    MovieActor {
        int MovieId PK, FK
        int ActorId PK, FK
        nvarchar Role "nullable, up to 200 characters"
    }
```

- Each details record belongs to one movie. A unique index on `MovieDetails.MovieId` limits each movie to one details record.
- A movie can have zero or more reviews. `CK_Reviews_Rating` enforces the inclusive range from 1 to 5.
- The composite key `MovieActor(MovieId, ActorId)` permits one link per movie and actor. A null role represents a link awaiting a role assignment.
- Deleting a movie cascades to its details, reviews, and cast links. Deleting an actor cascades to its cast links.

[SQL Server schema](https://github.com/RakivoJoy/ReEducation-MovieApi/blob/main/scripts/sql/movieapi-schema.sql) · [EF mappings](https://github.com/RakivoJoy/ReEducation-MovieApi/blob/main/src/MovieApi/Data/MovieDbContext.cs)
