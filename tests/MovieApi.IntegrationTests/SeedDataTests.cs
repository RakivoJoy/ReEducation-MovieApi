
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using MovieApi.Controllers;
using MovieApi.DTOs;
using MovieApi.Models;
using Xunit;

namespace MovieApi.IntegrationTests;

public class SeedDataTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DevelopmentStartupSeedsAnEmptyCatalogAndPreservesAnExistingCatalog(bool hasExistingMovie)
    {
        // Arrange
        await using var testDatabase = new SqlServerTestDatabase();
        await using var database = testDatabase.CreateContext();
        // Match deployment by provisioning the database before starting either host.
        await database.GetService<IRelationalDatabaseCreator>().CreateAsync();
        if (hasExistingMovie)
        {
            await database.Database.MigrateAsync();
            database.Movies.Add(new Movie { Title = "Existing movie", Year = 2024, Genre = "Drama", Duration = 100 });
            await database.SaveChangesAsync();
        }

        // Act: Start two hosts against the same database.
        var catalogs = await Task.WhenAll(
            Task.Run(() => StartAndReadCatalogAsync(testDatabase)),
            Task.Run(() => StartAndReadCatalogAsync(testDatabase)));
        var firstCatalog = catalogs[0];
        var secondCatalog = catalogs[1];

        // Assert
        Assert.Equal(firstCatalog.Select(movie => movie.Id).Order(), secondCatalog.Select(movie => movie.Id).Order());
        var movies = await database.Movies.AsNoTracking()
            .Include(movie => movie.Details)
            .Include(movie => movie.Reviews)
            .Include(movie => movie.Actors)
            .AsSplitQuery()
            .ToListAsync();

        if (hasExistingMovie)
        {
            Assert.Equal("Existing movie", Assert.Single(secondCatalog).Title);
            Assert.Null(Assert.Single(movies).Details);
            return;
        }

        Assert.Equal(2, secondCatalog.Length);
        Assert.Equal(2, movies.Count);
        Assert.All(movies, movie =>
        {
            Assert.NotNull(movie.Details);
            Assert.Equal(movie.Id, movie.Details.MovieId);
            Assert.NotEmpty(movie.Reviews);
            Assert.All(movie.Reviews, review => Assert.InRange(review.Rating, 1, 5));
            Assert.NotEmpty(movie.Actors);
        });
        var sharedActor = Assert.Single(movies[0].Actors.IntersectBy(movies[1].Actors.Select(actor => actor.Id), actor => actor.Id));
        Assert.Equal("Amy Adams", sharedActor.Name);
    }

    private static async Task<MovieDto[]> StartAndReadCatalogAsync(SqlServerTestDatabase testDatabase)
    {
        using var factory = new WebApplicationFactory<WeatherForecastController>();
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("SeedData:Enabled", "true");
            builder.UseSetting("ConnectionStrings:MovieApi", testDatabase.ConnectionString);
        });
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        return await client.GetFromJsonAsync<MovieDto[]>("/api/movies")
            ?? throw new InvalidOperationException("The movie catalog response was empty.");
    }
}
