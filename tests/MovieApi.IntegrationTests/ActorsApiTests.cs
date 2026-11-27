using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using MovieApi.Controllers;
using MovieApi.DTOs;
using Xunit;
using static MovieApi.IntegrationTests.MovieApiTestHost;

namespace MovieApi.IntegrationTests;

public class ActorsApiTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    [Fact]
    public async Task ActorCanBeCreatedUpdatedAndListed()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
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
    public async Task PutActorReturnsNotFoundWithoutCreatingActor()
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var update = new ActorUpdateDto { Name = "Tom Hanks", BirthYear = 1956 };

        // Act
        using var response = await client.PutAsJsonAsync("/api/actors/1", update);
        var actors = await client.GetFromJsonAsync<ActorDto[]>("/api/actors");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(actors);
        Assert.Empty(actors);
    }
}
