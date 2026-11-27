using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using MovieApi.Controllers;
using MovieApi.DTOs;
using MovieApi.Models;
using Xunit;
using static MovieApi.IntegrationTests.MovieApiTestHost;

namespace MovieApi.IntegrationTests;

public class MoviesApiTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    [Fact]
    public async Task GetMoviesReturnsEmptyListForEmptyCatalog()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);

        // Act
        var movies = await client.GetFromJsonAsync<MovieDto[]>(MoviesRoute);

        // Assert
        Assert.NotNull(movies);
        Assert.Empty(movies);
    }

    [Fact]
    public async Task GetMoviesReturnsSummariesWithoutRelatedData()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var catalog = await client.GetFromJsonAsync<JsonElement>(MoviesRoute);

        // Assert
        var summary = Assert.Single(catalog.EnumerateArray());
        Assert.Equal(movie.Id, summary.GetProperty("id").GetInt32());
        Assert.Equal(movie.Title, summary.GetProperty("title").GetString());
        Assert.False(summary.TryGetProperty("details", out _));
        Assert.False(summary.TryGetProperty("reviews", out _));
        Assert.False(summary.TryGetProperty("actors", out _));
    }

    [Fact]
    public async Task GetMovieReturnsDetailsReviewsAndActors()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var detail = await client.GetFromJsonAsync<MovieDetailDto>($"{MoviesRoute}/{movie.Id}");

        // Assert
        Assert.NotNull(detail);
        Assert.Equal(movie.Title, detail.Title);
        Assert.NotNull(detail.Details);
        Assert.Equal("English", detail.Details.Language);
        Assert.Equal(5, Assert.Single(detail.Reviews).Rating);
        Assert.Equal("Amy Adams", Assert.Single(detail.Actors).Name);
    }

    [Fact]
    public async Task PostMovieReturnsCreatedWithLocationAndPersistsMovie()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var request = new MovieCreateDto { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };

        // Act
        using var response = await client.PostAsJsonAsync(MoviesRoute, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var location = response.Headers.Location;
        Assert.NotNull(location);
        var saved = await client.GetFromJsonAsync<MovieDetailDto>(location);
        Assert.NotNull(saved);
        Assert.True(saved.Id > 0);
        Assert.Equal(request.Title, saved.Title);
        Assert.Null(saved.Details);
        Assert.Empty(saved.Reviews);
        Assert.Empty(saved.Actors);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task WriteMovieRejectsInvalidFields(string method)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var uri = method == "POST" ? MoviesRoute : $"{MoviesRoute}/{movie.Id}";
        using var request = new HttpRequestMessage(new HttpMethod(method), uri)
        {
            Content = JsonContent.Create(new { Title = "", Genre = "", Year = 1700, Duration = 0 })
        };

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Title", out _));
        Assert.True(errors.TryGetProperty("Genre", out _));
        Assert.True(errors.TryGetProperty("Year", out _));
        Assert.True(errors.TryGetProperty("Duration", out _));
    }

    [Fact]
    public async Task PutMovieUpdatesFieldsAndPreservesRelations()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var uri = $"{MoviesRoute}/{movie.Id}";
        var request = new MovieUpdateDto { Title = "Updated title", Year = 2017, Genre = "Drama", Duration = 120 };

        // Act
        using var response = await client.PutAsJsonAsync(uri, request);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var changed = await client.GetFromJsonAsync<MovieDetailDto>(uri);
        Assert.NotNull(changed);
        Assert.Equal(request.Title, changed.Title);
        Assert.Equal(request.Year, changed.Year);
        Assert.Equal(request.Genre, changed.Genre);
        Assert.Equal(request.Duration, changed.Duration);
        Assert.NotNull(changed.Details);
        Assert.Equal(movie.Details!.Id, changed.Details.Id);
        Assert.Equal(Assert.Single(movie.Reviews).Id, Assert.Single(changed.Reviews).Id);
        Assert.Equal(Assert.Single(movie.Actors).Id, Assert.Single(changed.Actors).Id);
    }

    [Fact]
    public async Task DeleteMovieRemovesExistingMovie()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var uri = $"{MoviesRoute}/{movie.Id}";

        // Act
        using var response = await client.DeleteAsync(uri);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var missing = await client.GetAsync(uri);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("genre=Science+fiction&year=2016&actor=Amy+Adams", true)]
    [InlineData("genre=Science+fiction&year=2016&actor=Tom+Hanks", false)]
    [InlineData("genre=Drama&year=2016&actor=Amy+Adams", false)]
    [InlineData("genre=Science+fiction&year=2022&actor=Amy+Adams", false)]
    public async Task GetMoviesCombinesFilters(string query, bool expectedMatch)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var movies = await client.GetFromJsonAsync<MovieDto[]>($"{MoviesRoute}?{query}");

        // Assert
        Assert.NotNull(movies);
        Assert.Equal(expectedMatch ? new[] { movie.Id } : [], movies.Select(item => item.Id));
    }

    [Fact]
    public async Task GetMovieDetailsReturnsMovieWithDetailsActorsAndReviews()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var details = await client.GetFromJsonAsync<MovieDetailDto>($"{MoviesRoute}/{movie.Id}/details");
        using var missing = await client.GetAsync($"{MoviesRoute}/{movie.Id + 1}/details");

        // Assert
        Assert.NotNull(details);
        Assert.Equal(movie.Id, details.Id);
        Assert.Equal(movie.Title, details.Title);
        Assert.Equal(movie.Details!.Synopsis, details.Details?.Synopsis);
        Assert.Equal(Assert.Single(movie.Actors).Name, Assert.Single(details.Actors).Name);
        Assert.Equal(Assert.Single(movie.Reviews).Rating, Assert.Single(details.Reviews).Rating);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task MovieDetailRoutesPreserveMissingDetailsAndOrderRelatedItems()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        await using var database = testDatabase.CreateContext();
        var movie = new Movie { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };
        var firstActor = new Actor { Name = "Jeremy Renner", BirthYear = 1971 };
        var secondActor = new Actor { Name = "Amy Adams", BirthYear = 1974 };
        database.AddRange(movie, firstActor, secondActor);
        await database.SaveChangesAsync();
        movie.Actors.Add(secondActor);
        movie.Actors.Add(firstActor);
        movie.Reviews.Add(new Review { ReviewerName = "Zoe", Comment = "First review", Rating = 5 });
        movie.Reviews.Add(new Review { ReviewerName = "Alex", Comment = "Second review", Rating = 4 });
        await database.SaveChangesAsync();
        var expectedReviewIds = movie.Reviews.Select(review => review.Id).Order().ToArray();
        var expectedActorIds = new[] { firstActor.Id, secondActor.Id }.Order().ToArray();

        // Act
        var detail = await client.GetFromJsonAsync<MovieDetailDto>($"{MoviesRoute}/{movie.Id}");
        var detailsRoute = await client.GetFromJsonAsync<MovieDetailDto>($"{MoviesRoute}/{movie.Id}/details");

        // Assert
        Assert.NotNull(detail);
        Assert.NotNull(detailsRoute);
        Assert.Null(detail.Details);
        Assert.Null(detailsRoute.Details);
        Assert.Equal(expectedReviewIds, detail.Reviews.Select(review => review.Id));
        Assert.Equal(expectedReviewIds, detailsRoute.Reviews.Select(review => review.Id));
        Assert.Equal(expectedActorIds, detail.Actors.Select(actor => actor.Id));
        Assert.Equal(expectedActorIds, detailsRoute.Actors.Select(actor => actor.Id));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MovieWriteReturnsNotFoundWhenMovieIsDeletedBeforeSave(bool update)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        await using var database = testDatabase.CreateContext();
        var movie = new Movie { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };
        var deleteMovie = new BeforeSave(async cancellationToken =>
        {
            await database.Movies.Where(candidate => candidate.Id == movie.Id).ExecuteDeleteAsync(cancellationToken);
        });
        using var app = await CreateAppAsync(factory, testDatabase, deleteMovie);
        database.Movies.Add(movie);
        await database.SaveChangesAsync();
        using var client = CreateClient(app);
        var movieRoute = $"{MoviesRoute}/{movie.Id}";

        // Act
        using var response = update
            ? await client.PutAsJsonAsync(movieRoute, new MovieUpdateDto
            {
                Title = "Updated title", Year = movie.Year, Genre = movie.Genre, Duration = movie.Duration
            })
            : await client.DeleteAsync(movieRoute);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(await database.Movies.AnyAsync(candidate => candidate.Id == movie.Id));
    }
}
