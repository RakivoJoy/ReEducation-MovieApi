using Microsoft.EntityFrameworkCore;
using MovieApi.Data;

namespace MovieApi.Extensions;

public static class SeedDataExtensions
{
    public static void SeedData(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment() || !app.Configuration.GetValue<bool>("SeedData:Enabled"))
        {
            return;
        }

        // The startup code needs a scope because the DbContext is a scoped service.
        using var scope = app.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<MovieDbContext>();
        database.Database.Migrate();

        // Keep the empty-catalog check and the inserts in the same SQLite write transaction.
        using var transaction = database.Database.BeginTransaction();
        if (database.Movies.Any())
        {
            return;
        }

        // EF saves the related entities and creates the join rows for the shared actor.
        database.Movies.AddRange(DemoMovieCatalog.Create());
        database.SaveChanges();
        transaction.Commit();
    }
}
