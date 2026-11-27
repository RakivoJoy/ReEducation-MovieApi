using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using MovieApi.Controllers;
using MovieApi.Models;
using Xunit;
using static MovieApi.IntegrationTests.MovieApiTestHost;

namespace MovieApi.IntegrationTests;

public class ApiErrorTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    [Theory]
    [InlineData("POST", "/api/actors", """{"name":"","birthYear":0}""", "Name", "BirthYear")]
    [InlineData("PUT", "/api/actors/1", """{"name":"","birthYear":0}""", "Name", "BirthYear")]
    [InlineData("POST", "/api/movies/1/reviews", """{"reviewerName":"","rating":6}""", "ReviewerName", "Rating")]
    public async Task InvalidRequestsReturnValidationErrors(
        string method, string path, string json, string requiredField, string rangeField)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
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
        using var app = await CreateAppAsync(factory, testDatabase);
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
        using var app = await CreateAppAsync(factory, testDatabase, concurrentWrite);
        database.AddRange(movie, actor);
        await database.SaveChangesAsync();
        using var client = CreateClient(app);

        // Act
        using var response = assigningCast
            ? await client.PostAsJsonAsync($"{MoviesRoute}/{movie.Id}/actors/{actor.Id}", new { Role = "Second role" })
            : await client.PostAsJsonAsync($"{MoviesRoute}/{movie.Id}/reviews", new { ReviewerName = "Alex", Comment = "Review", Rating = 5 });

        // Assert
        Assert.Equal(assigningCast ? HttpStatusCode.Conflict : HttpStatusCode.NotFound, response.StatusCode);
    }
}
