# MovieApi class diagram

```mermaid
classDiagram
    MoviesController ..> MovieDbContext
    MoviesController ..> CastAssignmentService
    MoviesController ..> MovieActorCreateDto
    MoviesController ..> MovieDto
    MoviesController ..> MovieDetailDto
    ActorsController ..> MovieDbContext
    ActorsController ..> ActorDto
    ReviewsController ..> MovieDbContext
    ReviewsController ..> ReviewDto
    CastAssignmentService ..> MovieDbContext
    MovieDetailDto --> MovieDetailsDto
    MovieDetailDto --> MovieCastMemberDto
    MovieDetailDto --> ReviewDto
    MovieDbContext ..> Movie
    MovieDbContext ..> Actor
    MovieDbContext ..> MovieDetails
    MovieDbContext ..> Review
    MovieDbContext ..> MovieActor
    Movie "1" --> "0..1" MovieDetails
    Movie "1" --> "0..*" Review
    Movie "1" --> "0..*" MovieActor
    MovieActor "0..*" --> "1" Actor
```

Controllers project EF Core entities into response DTOs. The detail response composes separate DTOs for supplementary information, reviews, and cast. `MovieActor` represents the stored relationship, while `MovieCastMemberDto` exposes the actor profile and role for a particular movie.

[Controllers](https://github.com/RakivoJoy/ReEducation-MovieApi/tree/main/src/MovieApi/Controllers) · [DTOs](https://github.com/RakivoJoy/ReEducation-MovieApi/tree/main/src/MovieApi/DTOs) · [EF mappings](https://github.com/RakivoJoy/ReEducation-MovieApi/blob/main/src/MovieApi/Data/MovieDbContext.cs)
