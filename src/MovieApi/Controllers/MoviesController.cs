using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.Models;

namespace MovieApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MoviesController(MovieDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Movie>>> GetMovies(CancellationToken cancellationToken)
    {
        return await context.Movies.AsNoTracking().ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Movie>> GetMovie(int id, CancellationToken cancellationToken)
    {
        var movie = await context.Movies.AsNoTracking()
            .SingleOrDefaultAsync(movie => movie.Id == id, cancellationToken);
        return movie is null ? NotFound() : movie;
    }

    [HttpPost]
    public async Task<ActionResult<Movie>> PostMovie(MovieRequest request, CancellationToken cancellationToken)
    {
        var movie = new Movie
        {
            Title = request.Title,
            Year = request.Year,
            Genre = request.Genre,
            Duration = request.Duration
        };
        context.Movies.Add(movie);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetMovie), new { id = movie.Id }, movie);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutMovie(int id, MovieRequest request, CancellationToken cancellationToken)
    {
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
            if (!await context.Movies.AnyAsync(movie => movie.Id == id, cancellationToken))
            {
                return NotFound();
            }

            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovie(int id, CancellationToken cancellationToken)
    {
        var movie = await context.Movies.FindAsync([id], cancellationToken);
        if (movie is null)
        {
            return NotFound();
        }

        context.Movies.Remove(movie);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return NotFound();
        }

        return NoContent();
    }
}
