using Microsoft.EntityFrameworkCore;
using MovieApi.Data;

namespace MovieApi.Extensions;

public static class SeedDataExtensions
{
    /// <summary>Applies migrations and adds demo movies when development seeding is enabled.</summary>
    /// <param name="app">The application whose configuration and services control seeding.</param>
    /// <remarks>The catalog is seeded only when it contains no movies.</remarks>
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

        using var transaction = database.Database.BeginTransaction();
        // The transaction owns the lock so a second startup checks the catalog only after the first commits or rolls back.
        database.Database.ExecuteSqlRaw("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = N'MovieApi.DemoCatalog',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
            IF @result < 0 THROW 50001, 'Could not acquire the demo catalog seed lock.', 1;
            """);
        // Preserve existing catalogs, including ones with only user-entered movies.
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
