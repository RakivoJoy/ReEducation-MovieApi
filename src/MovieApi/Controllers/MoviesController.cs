using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;
using MovieApi.Services;

namespace MovieApi.Controllers;

// ASP.NET Core replaces [controller] with the class name minus the Controller suffix.
// ApiController enables automatic request binding and returns HTTP 400 for invalid model state.
// Dependency injection supplies a scoped MovieDbContext through the primary constructor.
[Route("api/[controller]")]
[ApiController]
public class MoviesController(MovieDbContext context, CastAssignmentService castAssignment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovieDto>>> GetMovies(
        [FromQuery] string? genre, [FromQuery] int? year, [FromQuery] string? actor,
        CancellationToken cancellationToken)
    {
        IQueryable<Movie> movies = context.Movies;
        if (!string.IsNullOrWhiteSpace(genre))
        {
            movies = movies.Where(movie => movie.Genre == genre);
        }

        if (year.HasValue)
        {
            movies = movies.Where(movie => movie.Year == year.Value);
        }

        if (!string.IsNullOrWhiteSpace(actor))
        {
            movies = movies.Where(movie => movie.Actors.Any(castMember => castMember.Name == actor));
        }

        // Project to the summary DTO so the query selects only the catalog fields.
        return await movies
            .Select(movie => new MovieDto(movie.Id, movie.Title, movie.Year, movie.Genre, movie.Duration))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}/details")]
    public Task<ActionResult<MovieDetailDto>> GetMovieDetails(int id, CancellationToken cancellationToken) =>
        FindMovieDetailAsync(id, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<ActionResult<MovieDetailDto>> GetMovie(int id, CancellationToken cancellationToken) =>
        FindMovieDetailAsync(id, cancellationToken);

    private async Task<ActionResult<MovieDetailDto>> FindMovieDetailAsync(int id, CancellationToken cancellationToken)
    {
        // The DTO projection loads the two collections without exposing EF navigation properties.
        // Split queries avoid multiplying the review and actor rows in a single join.
        var movie = await context.Movies
            .Where(movie => movie.Id == id)
            .AsSplitQuery()
            .Select(movie => new MovieDetailDto(
                movie.Id, movie.Title, movie.Year, movie.Genre, movie.Duration,
                movie.Details == null ? null : new MovieDetailsDto(
                    movie.Details.Id, movie.Details.Synopsis, movie.Details.Language, movie.Details.Budget),
                movie.Reviews.OrderBy(review => review.Id)
                    .Select(review => new ReviewDto(review.Id, review.ReviewerName, review.Comment, review.Rating)).ToList(),
                movie.Cast.OrderBy(castMember => castMember.ActorId)
                    .Select(castMember => new ActorDto(castMember.ActorId, castMember.Actor.Name, castMember.Actor.BirthYear)
                    {
                        Role = castMember.Role
                    }).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
        return movie is null ? NotFound() : movie;
    }

    [HttpPost("{movieId:int}/actors/{actorId:int}")]
    public async Task<IActionResult> PostMovieActor(
        int movieId, int actorId, MovieActorCreateDto request, CancellationToken cancellationToken)
    {
        var result = await castAssignment.AssignRoleAsync(movieId, actorId, request.Role, cancellationToken);
        return result switch
        {
            CastAssignmentResult.Created => StatusCode(StatusCodes.Status201Created),
            CastAssignmentResult.Updated => NoContent(),
            CastAssignmentResult.ResourceMissing => NotFound(),
            CastAssignmentResult.RoleAlreadyAssigned => Conflict(),
            _ => throw new InvalidOperationException($"Unexpected cast assignment result: {result}")
        };
    }

    [HttpPost]
    public async Task<ActionResult<MovieDto>> PostMovie(MovieCreateDto request, CancellationToken cancellationToken)
    {
        // MovieCreateDto is the JSON input contract. Copying its fields leaves ID generation
        // to the database and keeps relationship changes outside this operation.
        var movie = new Movie
        {
            Title = request.Title,
            Year = request.Year,
            Genre = request.Genre,
            Duration = request.Duration
        };
        // Add registers a pending insert. SaveChangesAsync writes the row and populates its ID.
        context.Movies.Add(movie);
        await context.SaveChangesAsync(cancellationToken);

        // Return HTTP 201 with a Location header generated from the GET action and the new ID.
        var summary = new MovieDto(movie.Id, movie.Title, movie.Year, movie.Genre, movie.Duration);
        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, summary);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutMovie(int id, MovieUpdateDto request, CancellationToken cancellationToken)
    {
        // The route supplies the primary key. FindAsync returns a tracked entity so EF can
        // detect changes to its properties when SaveChangesAsync runs.
        var movie = await context.Movies.FindAsync([id], cancellationToken);
        if (movie is null)
        {
            return NotFound();
        }

        movie.Title = request.Title;
        movie.Year = request.Year;
        movie.Genre = request.Genre;
        movie.Duration = request.Duration;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A row can disappear between lookup and save. Return 404 if it is gone.
            // Rethrow a concurrency failure if the record still exists.
            if (!await context.Movies.AnyAsync(movie => movie.Id == id, cancellationToken))
            {
                return NotFound();
            }

            throw;
        }

        return NoContent(); // HTTP 204 means the update succeeded and has no response body.
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovie(int id, CancellationToken cancellationToken)
    {
        var movie = await context.Movies.FindAsync([id], cancellationToken);
        if (movie is null)
        {
            return NotFound();
        }

        // Remove registers a pending delete. The database applies its configured cascade rules on save.
        context.Movies.Remove(movie);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrent delete can remove the row after lookup but before this save.
            return NotFound();
        }

        return NoContent();
    }
}
