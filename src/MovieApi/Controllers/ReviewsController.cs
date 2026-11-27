using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;

namespace MovieApi.Controllers;

/// <summary>Exposes movie-scoped review collections and individual review resources.</summary>
[ApiController]
public class ReviewsController(MovieDbContext context) : ControllerBase
{
    private const int SqlServerConstraintViolation = 547;

    /// <summary>Lists reviews for a movie ordered by review identifier.</summary>
    /// <param name="movieId">The parent movie identifier.</param>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>The review list, which can be empty, or HTTP 404 if the movie is missing.</returns>
    [HttpGet("api/movies/{movieId:int}/reviews")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews(int movieId, CancellationToken cancellationToken)
    {
        var reviews = await context.Movies.Where(movie => movie.Id == movieId)
            .Select(movie => movie.Reviews.OrderBy(review => review.Id)
                .Select(review => new ReviewDto(review.Id, review.ReviewerName, review.Comment, review.Rating)).ToList())
            .SingleOrDefaultAsync(cancellationToken);

        return reviews is null ? NotFound() : reviews;
    }

    /// <summary>Adds a review to a movie.</summary>
    /// <param name="movieId">The parent movie identifier.</param>
    /// <param name="request">The reviewer name, comment, and rating.</param>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>HTTP 201 with the review and its location, or 404 if the movie is missing.</returns>
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

    /// <summary>Reads an individual review by its own identifier.</summary>
    /// <param name="id">The review identifier.</param>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>The review or HTTP 404.</returns>
    [HttpGet("api/reviews/{id:int}")]
    public async Task<ActionResult<ReviewDto>> GetReview(int id, CancellationToken cancellationToken)
    {
        var review = await context.Reviews.Where(review => review.Id == id)
            .Select(review => new ReviewDto(review.Id, review.ReviewerName, review.Comment, review.Rating))
            .SingleOrDefaultAsync(cancellationToken);

        return review is null ? NotFound() : review;
    }

    /// <summary>Deletes an individual review by its own identifier.</summary>
    /// <param name="id">The review identifier.</param>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>HTTP 204 on success or 404 if the review is missing.</returns>
    [HttpDelete("api/reviews/{id:int}")]
    public async Task<IActionResult> DeleteReview(int id, CancellationToken cancellationToken)
    {
        var deleted = await context.Reviews.Where(review => review.Id == id).ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? NotFound() : NoContent();
    }
}
