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
    [Fact]
    public async Task MoviesSupportsCreateReadUpdateAndDelete()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        using var testFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<MovieDbContext>>();
            services.AddDbContext<MovieDbContext>(options => options.UseSqlite(connection));
        }));
        using var client = testFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        await using (var scope = testFactory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MovieDbContext>().Database.MigrateAsync();
        }

        const string route = "/api/movies";
        var movie = new { Title = "Arrival", Year = 2016, Genre = "Science fiction", Duration = 116 };

        // Act and assert each step of the HTTP workflow.
        Assert.Empty(await client.GetFromJsonAsync<MovieDto[]>(route) ?? throw new InvalidOperationException());

        using var created = await client.PostAsJsonAsync(route, movie);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location;
        Assert.NotNull(location);
        var saved = await client.GetFromJsonAsync<MovieDetailDto>(location);
        Assert.NotNull(saved);
        Assert.True(saved.Id > 0);
        Assert.Equal(movie.Title, saved.Title);
        Assert.Null(saved.Details);
        Assert.Empty(saved.Reviews);
        Assert.Empty(saved.Actors);
        var catalog = await client.GetFromJsonAsync<JsonElement>(route);
        var summary = Assert.Single(catalog.EnumerateArray());
        Assert.Equal(movie.Title, summary.GetProperty("title").GetString());
        Assert.False(summary.TryGetProperty("details", out _));
        Assert.False(summary.TryGetProperty("reviews", out _));
        Assert.False(summary.TryGetProperty("actors", out _));

        await using (var scope = testFactory.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
            var storedMovie = await database.Movies.SingleAsync(item => item.Id == saved.Id);
            storedMovie.Details = new MovieDetails { Synopsis = "A linguist makes first contact.", Language = "English", Budget = 47_000_000m };
            storedMovie.Reviews.Add(new Review { ReviewerName = "Alex", Comment = "Thoughtful", Rating = 5 });
            storedMovie.Actors.Add(new Actor { Name = "Amy Adams", BirthYear = 1974 });
            await database.SaveChangesAsync();
        }

        var detail = await client.GetFromJsonAsync<JsonElement>(location);
        Assert.Equal("English", detail.GetProperty("details").GetProperty("language").GetString());
        Assert.Equal(5, Assert.Single(detail.GetProperty("reviews").EnumerateArray()).GetProperty("rating").GetInt32());
        Assert.Equal("Amy Adams", Assert.Single(detail.GetProperty("actors").EnumerateArray()).GetProperty("name").GetString());

        var invalidMovie = movie with { Title = "", Genre = "", Year = 1700, Duration = 0 };
        using var rejectedCreate = await client.PostAsJsonAsync(route, invalidMovie);
        using var rejectedUpdate = await client.PutAsJsonAsync(location, invalidMovie);
        foreach (var response in new[] { rejectedCreate, rejectedUpdate })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
            var errors = problem.GetProperty("errors");
            Assert.True(errors.TryGetProperty("Title", out _));
            Assert.True(errors.TryGetProperty("Genre", out _));
            Assert.True(errors.TryGetProperty("Year", out _));
            Assert.True(errors.TryGetProperty("Duration", out _));
        }

        using var updated = await client.PutAsJsonAsync(location, movie with { Title = "Updated title" });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var changed = await client.GetFromJsonAsync<MovieDetailDto>(location);
        Assert.NotNull(changed);
        Assert.Equal("Updated title", changed.Title);
        Assert.NotNull(changed.Details);
        Assert.Single(changed.Reviews);
        Assert.Single(changed.Actors);

        using var deleted = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
