using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MovieApi.Controllers;
using MovieApi.Data;
using MovieApi.Models;

namespace MovieApi.IntegrationTests;

internal static class MovieApiTestHost
{
    internal const string MoviesRoute = "/api/movies";

    internal sealed class BeforeSave(Func<CancellationToken, Task> concurrentWrite) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await concurrentWrite(cancellationToken);
            return result;
        }
    }

    internal static async Task<WebApplicationFactory<WeatherForecastController>> CreateAppAsync(
        WebApplicationFactory<WeatherForecastController> factory,
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

    internal static HttpClient CreateClient(WebApplicationFactory<WeatherForecastController> app) =>
        app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    internal static async Task<Movie> AddMovieWithRelationsAsync(WebApplicationFactory<WeatherForecastController> app)
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
