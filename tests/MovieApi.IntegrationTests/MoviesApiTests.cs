using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MovieApi.Controllers;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;
using Xunit;

namespace MovieApi.IntegrationTests;

public class MoviesApiTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    private const string Route = "/api/movies";

    [Fact]
    public async Task GetMoviesReturnsEmptyListForEmptyCatalog()
    {
        // Arrange
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);

        // Act
        var movies = await client.GetFromJsonAsync<MovieDto[]>(Route);

        // Assert
        Assert.NotNull(movies);
        Assert.Empty(movies);
    }

    [Fact]
    public async Task GetMoviesReturnsSummariesWithoutRelatedData()
    {
        // Arrange
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var catalog = await client.GetFromJsonAsync<JsonElement>(Route);

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
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var detail = await client.GetFromJsonAsync<MovieDetailDto>($"{Route}/{movie.Id}");

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
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var request = new MovieCreateDto { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };

        // Act
        using var response = await client.PostAsJsonAsync(Route, request);

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
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var uri = method == "POST" ? Route : $"{Route}/{movie.Id}";
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
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var uri = $"{Route}/{movie.Id}";
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
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var uri = $"{Route}/{movie.Id}";

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
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var movies = await client.GetFromJsonAsync<MovieDto[]>($"{Route}?{query}");

        // Assert
        Assert.NotNull(movies);
        Assert.Equal(expectedMatch ? new[] { movie.Id } : [], movies.Select(item => item.Id));
    }

    [Fact]
    public async Task GetMovieDetailsReturnsTheDetailsRecord()
    {
        // Arrange
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var details = await client.GetFromJsonAsync<MovieDetailsDto>($"{Route}/{movie.Id}/details");
        using var missing = await client.GetAsync($"{Route}/{movie.Id + 1}/details");

        // Assert
        Assert.Equal(movie.Details!.Synopsis, details?.Synopsis);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ActorCanBeCreatedUpdatedAndListed()
    {
        // Arrange
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        const string actorsRoute = "/api/actors";

        // Act
        using var created = await client.PostAsJsonAsync(actorsRoute, new { Name = "Tom Hanks", BirthYear = 1956 });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = Assert.IsType<Uri>(created.Headers.Location, exactMatch: false);
        var actor = await client.GetFromJsonAsync<ActorDto>(location);
        using var updated = await client.PutAsJsonAsync(location, new { Name = "Thomas Hanks", BirthYear = 1956 });
        var actors = await client.GetFromJsonAsync<ActorDto[]>(actorsRoute);

        // Assert
        Assert.Equal("Tom Hanks", actor?.Name);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        Assert.Equal("Thomas Hanks", Assert.Single(actors!).Name);
    }

    [Fact]
    public async Task ReviewCanBeCreatedListedAndDeleted()
    {
        // Arrange
        await using var connection = CreateConnection();
        using var app = await CreateAppAsync(connection);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var reviewsRoute = $"{Route}/{movie.Id}/reviews";
        var request = new { ReviewerName = "Robin", Comment = "Engaging", Rating = 4 };

        // Act
        using var missingMovie = await client.PostAsJsonAsync($"{Route}/{movie.Id + 1}/reviews", request);
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

    private static SqliteConnection CreateConnection() => new("Data Source=:memory:;Foreign Keys=True");

    private async Task<WebApplicationFactory<WeatherForecastController>> CreateAppAsync(SqliteConnection connection)
    {
        // Keep this connection open for the lifetime of the test's private database.
        await connection.OpenAsync();
        var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<MovieDbContext>>();
            services.AddDbContext<MovieDbContext>(options => options.UseSqlite(connection));
        }));

        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<MovieDbContext>().Database.MigrateAsync();
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    private static HttpClient CreateClient(WebApplicationFactory<WeatherForecastController> app) =>
        app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    private static async Task<Movie> AddMovieWithRelationsAsync(WebApplicationFactory<WeatherForecastController> app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        var movie = new Movie
        {
            Title = "Arrival",
            Year = 2016,
            Genre = "Science fiction",
            Duration = 116,
            Details = new MovieDetails { Synopsis = "A linguist makes first contact.", Language = "English", Budget = 47_000_000m },
            Reviews = { new Review { ReviewerName = "Alex", Comment = "Thoughtful", Rating = 5 } },
            Actors = { new Actor { Name = "Amy Adams", BirthYear = 1974 } }
        };
        database.Movies.Add(movie);
        await database.SaveChangesAsync();
        return movie;
    }
}
