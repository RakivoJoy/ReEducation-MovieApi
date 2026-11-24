using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MovieApi.Controllers;
using MovieApi.Data;
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
        Assert.Empty(await client.GetFromJsonAsync<Movie[]>(route) ?? throw new InvalidOperationException());

        using var created = await client.PostAsJsonAsync(route, movie);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = created.Headers.Location;
        Assert.NotNull(location);
        var saved = await client.GetFromJsonAsync<Movie>(location);
        Assert.NotNull(saved);
        Assert.True(saved.Id > 0);
        Assert.Equal(movie.Title, saved.Title);
        Assert.Single(await client.GetFromJsonAsync<Movie[]>(route) ?? throw new InvalidOperationException());

        using var updated = await client.PutAsJsonAsync(location, movie with { Title = "Updated title" });
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        var changed = await client.GetFromJsonAsync<Movie>(location);
        Assert.NotNull(changed);
        Assert.Equal("Updated title", changed.Title);

        using var deleted = await client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var missing = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
