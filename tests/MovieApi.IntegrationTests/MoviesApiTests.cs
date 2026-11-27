using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
    private static readonly string[] ActorJsonProperties = ["birthYear", "id", "name"];

    [Fact]
    public async Task GetMoviesReturnsEmptyListForEmptyCatalog()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var movies = await client.GetFromJsonAsync<MovieDto[]>($"{Route}?{query}");

        // Assert
        Assert.NotNull(movies);
        Assert.Equal(expectedMatch ? new[] { movie.Id } : [], movies.Select(item => item.Id));
    }

    [Fact]
    public async Task GetMovieDetailsReturnsMovieWithDetailsActorsAndReviews()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);

        // Act
        var details = await client.GetFromJsonAsync<MovieDetailDto>($"{Route}/{movie.Id}/details");
        using var missing = await client.GetAsync($"{Route}/{movie.Id + 1}/details");

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
        using var app = await CreateAppAsync(testDatabase);
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
        var detail = await client.GetFromJsonAsync<MovieDetailDto>($"{Route}/{movie.Id}");
        var detailsRoute = await client.GetFromJsonAsync<MovieDetailDto>($"{Route}/{movie.Id}/details");

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

    [Fact]
    public async Task ActorCanBeCreatedUpdatedAndListed()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostMovieActorSavesRoleForNewAndExistingLinks(bool existingLink)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var actor = Assert.Single(movie.Actors);
        if (!existingLink)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
            actor = new Actor { Name = "Jeremy Renner", BirthYear = 1971 };
            database.Actors.Add(actor);
            await database.SaveChangesAsync();
        }
        var movieRoute = $"{Route}/{movie.Id}";
        var castRoute = $"{movieRoute}/actors/{actor.Id}";
        var before = await client.GetFromJsonAsync<MovieDetailDto>(movieRoute);

        // Act
        using var invalid = await client.PostAsJsonAsync(castRoute, new { Role = "" });
        using var missingActor = await client.PostAsJsonAsync($"{movieRoute}/actors/{actor.Id + 1}", new { Role = "Lead" });
        using var missingMovie = await client.PostAsJsonAsync($"{Route}/{movie.Id + 1}/actors/{actor.Id}", new { Role = "Lead" });
        using var saved = await client.PostAsJsonAsync(castRoute, new { Role = "Lead" });
        using var duplicate = await client.PostAsJsonAsync(castRoute, new { Role = "Other" });
        var after = await client.GetFromJsonAsync<MovieDetailDto>(movieRoute);

        // Assert
        Assert.NotNull(before);
        var priorLink = before.Actors.SingleOrDefault(item => item.Id == actor.Id);
        Assert.Equal(existingLink, priorLink is not null);
        Assert.Null(priorLink?.Role);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingActor.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingMovie.StatusCode);
        Assert.Equal(existingLink ? HttpStatusCode.NoContent : HttpStatusCode.Created, saved.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.NotNull(after);
        Assert.Equal("Lead", Assert.Single(after.Actors, item => item.Id == actor.Id).Role);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/details")]
    public async Task MovieCastJsonIncludesOnlyAssignedRoles(string routeSuffix)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var actor = Assert.Single(movie.Actors);
        var detailsRoute = $"{Route}/{movie.Id}{routeSuffix}";

        // Act
        var before = await client.GetFromJsonAsync<JsonElement>(detailsRoute);
        using var saved = await client.PostAsJsonAsync($"{Route}/{movie.Id}/actors/{actor.Id}", new { Role = "Lead" });
        var after = await client.GetFromJsonAsync<JsonElement>(detailsRoute);
        var actorJson = await client.GetFromJsonAsync<JsonElement>($"/api/actors/{actor.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        var castMember = Assert.Single(after.GetProperty("actors").EnumerateArray());
        Assert.False(Assert.Single(before.GetProperty("actors").EnumerateArray()).TryGetProperty("role", out _));
        Assert.Equal(actor.Id, castMember.GetProperty("id").GetInt32());
        Assert.Equal(actor.Name, castMember.GetProperty("name").GetString());
        Assert.Equal(actor.BirthYear, castMember.GetProperty("birthYear").GetInt32());
        Assert.Equal("Lead", castMember.GetProperty("role").GetString());
        Assert.Equal(ActorJsonProperties, actorJson.EnumerateObject().Select(property => property.Name).Order());
    }

    [Theory]
    [InlineData("POST", "/api/actors", """{"name":"","birthYear":0}""", "Name", "BirthYear")]
    [InlineData("PUT", "/api/actors/1", """{"name":"","birthYear":0}""", "Name", "BirthYear")]
    [InlineData("POST", "/api/movies/1/reviews", """{"reviewerName":"","rating":6}""", "ReviewerName", "Rating")]
    public async Task InvalidRequestsReturnValidationErrors(
        string method, string path, string json, string requiredField, string rangeField)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
        using var client = CreateClient(app);
        using var request = new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty(requiredField, out _));
        Assert.True(errors.TryGetProperty(rangeField, out _));
    }

    [Theory]
    [InlineData("/api/movies/1")]
    [InlineData("/api/movies/1/reviews")]
    [InlineData("/api/actors/1")]
    public async Task GetMissingResourceReturnsNotFound(string path)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
        using var client = CreateClient(app);

        // Act
        using var response = await client.GetAsync(path);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostMapsSqlServerConstraintFailuresAfterAConcurrentWrite(bool assigningCast)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        await using var database = testDatabase.CreateContext();
        var movie = new Movie { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };
        var actor = new Actor { Name = "Amy Adams", BirthYear = 1974 };
        var concurrentWrite = new BeforeSave(async cancellationToken =>
        {
            if (assigningCast)
            {
                database.MovieActors.Add(new MovieActor { MovieId = movie.Id, ActorId = actor.Id, Role = "First role" });
                await database.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await database.Movies.Where(item => item.Id == movie.Id).ExecuteDeleteAsync(cancellationToken);
            }
        });
        using var app = await CreateAppAsync(testDatabase, concurrentWrite);
        database.AddRange(movie, actor);
        await database.SaveChangesAsync();
        using var client = CreateClient(app);

        // Act
        using var response = assigningCast
            ? await client.PostAsJsonAsync($"{Route}/{movie.Id}/actors/{actor.Id}", new { Role = "Second role" })
            : await client.PostAsJsonAsync($"{Route}/{movie.Id}/reviews", new { ReviewerName = "Alex", Comment = "Review", Rating = 5 });

        // Assert
        Assert.Equal(assigningCast ? HttpStatusCode.Conflict : HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMovieActorReturnsNotFoundWhenActorDisappearsBeforeInsert()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        await using var database = testDatabase.CreateContext();
        var actor = new Actor { Name = "Amy Adams", BirthYear = 1974 };
        var movie = new Movie { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };
        var deleteActor = new BeforeSave(async cancellationToken =>
        {
            await database.Actors.Where(candidate => candidate.Id == actor.Id).ExecuteDeleteAsync(cancellationToken);
        });
        using var app = await CreateAppAsync(testDatabase, deleteActor);
        database.AddRange(movie, actor);
        await database.SaveChangesAsync();
        using var client = CreateClient(app);

        // Act
        using var response = await client.PostAsJsonAsync($"{Route}/{movie.Id}/actors/{actor.Id}", new { Role = "Louise" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await database.MovieActors.ToListAsync());
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
        using var app = await CreateAppAsync(testDatabase, deleteMovie);
        database.Movies.Add(movie);
        await database.SaveChangesAsync();
        using var client = CreateClient(app);
        var movieRoute = $"{Route}/{movie.Id}";

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

    [Fact]
    public async Task GetReviewsDistinguishesAnEmptyCollectionFromAMissingMovie()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(testDatabase);
        using var client = CreateClient(app);
        await using var database = testDatabase.CreateContext();
        var movie = new Movie { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };
        database.Movies.Add(movie);
        await database.SaveChangesAsync();

        // Act
        var reviews = await client.GetFromJsonAsync<ReviewDto[]>($"{Route}/{movie.Id}/reviews");
        using var missing = await client.GetAsync($"{Route}/{movie.Id + 1}/reviews");

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

    private sealed class BeforeSave(Func<CancellationToken, Task> concurrentWrite) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await concurrentWrite(cancellationToken);
            return result;
        }
    }

    private async Task<WebApplicationFactory<WeatherForecastController>> CreateAppAsync(
        SqlServerTestDatabase testDatabase, SaveChangesInterceptor? interceptor = null)
    {
        var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:MovieApi", testDatabase.ConnectionString);
            if (interceptor is not null)
            {
                builder.ConfigureTestServices(services =>
                    services.ConfigureDbContext<MovieDbContext>(options => options.AddInterceptors(interceptor)));
            }
        });

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
