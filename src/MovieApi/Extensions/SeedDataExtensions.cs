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

        using var transaction = database.Database.BeginTransaction();
        // Serialize startup seeders until the catalog check and inserts have committed.
        database.Database.ExecuteSqlRaw("""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = N'MovieApi.DemoCatalog',
                @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;
            IF @result < 0 THROW 50001, 'Could not acquire the demo catalog seed lock.', 1;
            """);
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
