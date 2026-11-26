using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.Models;
using Xunit;

namespace MovieApi.IntegrationTests;

public sealed class MovieDatabaseTests
{
    [Fact]
    public async Task MovieDetailsRejectsSecondRecordForSameMovie()
    {
        // Arrange
        await using var database = await CreateDatabaseAsync();
        var movie = CreateMovie("First movie");
        movie.Details = new MovieDetails { Synopsis = "Synopsis", Language = "English", Budget = 1000m };
        database.Movies.Add(movie);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var duplicate = new MovieDetails
        {
            MovieId = movie.Id, Synopsis = "Another synopsis", Language = "English", Budget = 2000m
        };
        database.MovieDetails.Add(duplicate);

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());

        // Assert
        var sqliteError = Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Equal(2067, sqliteError.SqliteExtendedErrorCode); // SQLITE_CONSTRAINT_UNIQUE
    }

    [Fact]
    public async Task MoviePersistsMultipleReviewsAndActorLinks()
    {
        // Arrange
        await using var database = await CreateDatabaseAsync();
        var movie = CreateMovie("First movie");
        var otherMovie = CreateMovie("Second movie");
        var sharedActor = new Actor { Name = "Shared actor", BirthYear = 1984 };
        movie.Actors.Add(sharedActor);
        movie.Actors.Add(new Actor { Name = "Another actor", BirthYear = 1990 });
        otherMovie.Actors.Add(sharedActor);
        movie.Reviews.Add(new Review { ReviewerName = "First reviewer", Comment = "Review", Rating = 1 });
        movie.Reviews.Add(new Review { ReviewerName = "Second reviewer", Comment = "Review", Rating = 5 });
        database.Movies.AddRange(movie, otherMovie);
        int[] expectedRatings = [1, 5];
        string[] expectedActorNames = ["Another actor", "Shared actor"];

        // Act
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();
        var savedMovie = await database.Movies.Include(item => item.Reviews).Include(item => item.Actors)
            .AsSplitQuery().SingleAsync(item => item.Id == movie.Id);
        var savedActor = await database.Actors.Include(item => item.Movies)
            .SingleAsync(item => item.Id == sharedActor.Id);

        // Assert
        Assert.Equal(expectedRatings, savedMovie.Reviews.Select(review => review.Rating).Order());
        Assert.Equal(expectedActorNames, savedMovie.Actors.Select(actor => actor.Name).Order(StringComparer.Ordinal));
        Assert.Equal(new[] { movie.Id, otherMovie.Id }.Order(), savedActor.Movies.Select(item => item.Id).Order());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task ReviewRejectsRatingOutsideAllowedRange(int rating)
    {
        // Arrange
        await using var database = await CreateDatabaseAsync();
        var movie = CreateMovie("First movie");
        movie.Reviews.Add(new Review { ReviewerName = "Reviewer", Comment = "Review", Rating = rating });
        database.Movies.Add(movie);

        // Act
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => database.SaveChangesAsync());

        // Assert
        var sqliteError = Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Equal(275, sqliteError.SqliteExtendedErrorCode); // SQLITE_CONSTRAINT_CHECK
    }

    private static async Task<MovieDbContext> CreateDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<MovieDbContext>()
            .UseSqlite("Data Source=:memory:;Foreign Keys=True").Options;
        var database = new MovieDbContext(options);
        try
        {
            // SQLite keeps the in-memory database alive as long as this connection stays open.
            await database.Database.OpenConnectionAsync();
            await database.Database.MigrateAsync();
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    private static Movie CreateMovie(string title) => new()
    {
        Title = title,
        Year = 2024,
        Genre = "Drama",
        Duration = 110
    };
}
