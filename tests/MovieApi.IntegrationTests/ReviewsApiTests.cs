using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using MovieApi.Controllers;
using MovieApi.DTOs;
using MovieApi.Models;
using Xunit;
using static MovieApi.IntegrationTests.MovieApiTestHost;

namespace MovieApi.IntegrationTests;

public class ReviewsApiTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    [Fact]
    public async Task ReviewCanBeCreatedListedAndDeleted()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var reviewsRoute = $"{MoviesRoute}/{movie.Id}/reviews";
        var request = new { ReviewerName = "Robin", Comment = "Engaging", Rating = 4 };

        // Act
        using var missingMovie = await client.PostAsJsonAsync($"{MoviesRoute}/{movie.Id + 1}/reviews", request);
        using var created = await client.PostAsJsonAsync(reviewsRoute, request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location;
        Assert.NotNull(location);
        var review = await client.GetFromJsonAsync<ReviewDto>(location);
        var reviews = await client.GetFromJsonAsync<ReviewDto[]>(reviewsRoute);
        using var deleted = await client.DeleteAsync(location);
        using var missingReview = await client.GetAsync(location);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, missingMovie.StatusCode);
        Assert.Equal(4, review?.Rating);
        Assert.Contains(review!, reviews!);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingReview.StatusCode);
    }

    [Fact]
    public async Task GetReviewsDistinguishesAnEmptyCollectionFromAMissingMovie()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        await using var database = testDatabase.CreateContext();
        var movie = new Movie { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };
        database.Movies.Add(movie);
        await database.SaveChangesAsync();

        // Act
        var reviews = await client.GetFromJsonAsync<ReviewDto[]>($"{MoviesRoute}/{movie.Id}/reviews");
        using var missing = await client.GetAsync($"{MoviesRoute}/{movie.Id + 1}/reviews");

        // Assert
        Assert.NotNull(reviews);
        Assert.Empty(reviews);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PostReviewPropagatesRatingConstraintFailureWhenMovieExists()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        await using var database = testDatabase.CreateContext();
        await database.Database.MigrateAsync();
        var movie = new Movie { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };
        database.Movies.Add(movie);
        await database.SaveChangesAsync();
        var controller = new ReviewsController(database);
        var review = new ReviewCreateDto { ReviewerName = "Alex", Comment = "Invalid rating", Rating = 0 };

        // Act and assert. Calling the action directly bypasses HTTP validation to exercise the SQL constraint.
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            controller.PostReview(movie.Id, review, CancellationToken.None));
        Assert.Empty(await database.Reviews.ToListAsync());
    }
}
