using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;

namespace MovieApi.Controllers;

[ApiController]
public class ReviewsController(MovieDbContext context) : ControllerBase
{
    private const int SqlServerConstraintViolation = 547;

    [HttpGet("api/movies/{movieId:int}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int movieId, CancellationToken cancellationToken)
    {
        var reviews = await context.Movies.Where(movie => movie.Id == movieId)
            .Select(movie => movie.Reviews.OrderBy(review => review.Id)
                .Select(review => new ReviewDto(review.Id, review.ReviewerName, review.Comment, review.Rating)).ToList())
            .SingleOrDefaultAsync(cancellationToken);

        return reviews is null ? NotFound() : reviews;
    }

    [HttpPost("api/movies/{movieId:int}/reviews")]
    public async Task<ActionResult<ReviewDto>> PostReview(
        int movieId, ReviewCreateDto request, CancellationToken cancellationToken)
    {
        if (!await context.Movies.AnyAsync(movie => movie.Id == movieId, cancellationToken))
        {
            return NotFound();
        }

        var review = new Review
        {
            MovieId = movieId,
            ReviewerName = request.ReviewerName,
            Comment = request.Comment,
            Rating = request.Rating
        };
        context.Reviews.Add(review);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: SqlServerConstraintViolation })
        {
            // The movie foreign key is enforced even when a delete races this insert.
            if (!await context.Movies.AnyAsync(movie => movie.Id == movieId, cancellationToken))
            {
                return NotFound();
            }

            throw;
        }

        return CreatedAtAction(nameof(GetReview), new { id = review.Id },
            new ReviewDto(review.Id, review.ReviewerName, review.Comment, review.Rating));
    }

    [HttpGet("api/reviews/{id:int}")]
    public async Task<ActionResult<ReviewDto>> GetReview(int id, CancellationToken cancellationToken)
    {
        var review = await context.Reviews.Where(review => review.Id == id)
            .Select(review => new ReviewDto(review.Id, review.ReviewerName, review.Comment, review.Rating))
            .SingleOrDefaultAsync(cancellationToken);

        return review is null ? NotFound() : review;
    }

    [HttpDelete("api/reviews/{id:int}")]
    public async Task<IActionResult> DeleteReview(int id, CancellationToken cancellationToken)
    {
        var deleted = await context.Reviews.Where(review => review.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? NotFound() : NoContent();
    }
}
