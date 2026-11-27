using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieApi.Controllers;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;
using Xunit;
using static MovieApi.IntegrationTests.MovieApiTestHost;

namespace MovieApi.IntegrationTests;

public class CastApiTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    private static readonly string[] ActorJsonProperties = ["birthYear", "id", "name"];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostMovieActorSavesRoleForNewAndExistingLinks(bool existingLink)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        using var app = await CreateAppAsync(factory, testDatabase);
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
        var movieRoute = $"{MoviesRoute}/{movie.Id}";
        var castRoute = $"{movieRoute}/actors/{actor.Id}";
        var before = await client.GetFromJsonAsync<MovieDetailDto>(movieRoute);

        // Act
        using var invalid = await client.PostAsJsonAsync(castRoute, new { Role = "" });
        using var missingActor = await client.PostAsJsonAsync($"{movieRoute}/actors/{actor.Id + 1}", new { Role = "Lead" });
        using var missingMovie = await client.PostAsJsonAsync($"{MoviesRoute}/{movie.Id + 1}/actors/{actor.Id}", new { Role = "Lead" });
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
        using var app = await CreateAppAsync(factory, testDatabase);
        using var client = CreateClient(app);
        var movie = await AddMovieWithRelationsAsync(app);
        var actor = Assert.Single(movie.Actors);
        var detailsRoute = $"{MoviesRoute}/{movie.Id}{routeSuffix}";

        // Act
        var before = await client.GetFromJsonAsync<JsonElement>(detailsRoute);
        using var saved = await client.PostAsJsonAsync($"{MoviesRoute}/{movie.Id}/actors/{actor.Id}", new { Role = "Lead" });
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
        using var app = await CreateAppAsync(factory, testDatabase, deleteActor);
        database.AddRange(movie, actor);
        await database.SaveChangesAsync();
        using var client = CreateClient(app);

        // Act
        using var response = await client.PostAsJsonAsync($"{MoviesRoute}/{movie.Id}/actors/{actor.Id}", new { Role = "Louise" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await database.MovieActors.ToListAsync());
    }
}
