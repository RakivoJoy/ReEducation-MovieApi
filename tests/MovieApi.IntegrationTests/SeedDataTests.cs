
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
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

public class SeedDataTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DevelopmentStartupSeedsAnEmptyCatalogAndPreservesAnExistingCatalog(bool hasExistingMovie)
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<MovieDbContext>().UseSqlite(connection).Options;
        await using var database = new MovieDbContext(options);
        if (hasExistingMovie)
        {
            await database.Database.MigrateAsync();
            database.Movies.Add(new Movie { Title = "Existing movie", Year = 2024, Genre = "Drama", Duration = 100 });
            await database.SaveChangesAsync();
        }

        // Act: Start the host twice against the same database.
        var firstCatalog = await StartAndReadCatalogAsync(connection);
        var secondCatalog = await StartAndReadCatalogAsync(connection);

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

    private async Task<MovieDto[]> StartAndReadCatalogAsync(SqliteConnection connection)
    {
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("SeedData:Enabled", "true");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<MovieDbContext>>();
                services.AddDbContext<MovieDbContext>(options => options.UseSqlite(connection));
            });
        });
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        return await client.GetFromJsonAsync<MovieDto[]>("/api/movies")
            ?? throw new InvalidOperationException("The movie catalog response was empty.");
    }
}
