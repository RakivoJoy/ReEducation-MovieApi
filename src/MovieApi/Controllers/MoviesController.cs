using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.Models;

namespace MovieApi.Controllers;

// ASP.NET Core replaces [controller] with the class name minus the Controller suffix.
// ApiController enables automatic request binding and returns HTTP 400 for invalid model state.
// Dependency injection supplies a scoped MovieDbContext through the primary constructor.
[Route("api/[controller]")]
[ApiController]
public class MoviesController(MovieDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Movie>>> GetMovies(CancellationToken cancellationToken)
    {
        // AsNoTracking skips EF change tracking for this read-only query.
        // ToListAsync executes the SQL, using the cancellation token for an aborted request.
        return await context.Movies.AsNoTracking().ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Movie>> GetMovie(int id, CancellationToken cancellationToken)
    {
        // The int route constraint matches only integer IDs. A matching row becomes HTTP 200.
        // ActionResult<Movie> also lets the action return HTTP 404 when the query finds no row.
        var movie = await context.Movies.AsNoTracking()
            .SingleOrDefaultAsync(movie => movie.Id == id, cancellationToken);
        return movie is null ? NotFound() : movie;
    }

    [HttpPost]
    public async Task<ActionResult<Movie>> PostMovie(MovieRequest request, CancellationToken cancellationToken)
    {
        // MovieRequest is the JSON input contract. Copying its fields leaves ID generation
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
        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutMovie(int id, MovieRequest request, CancellationToken cancellationToken)
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
