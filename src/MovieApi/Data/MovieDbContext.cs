using Microsoft.EntityFrameworkCore;
using MovieApi.Models;

namespace MovieApi.Data;

public class MovieDbContext(DbContextOptions<MovieDbContext> options) : DbContext(options)
{
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<MovieDetails> MovieDetails => Set<MovieDetails>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Actor> Actors => Set<Actor>();
    public DbSet<MovieActor> MovieActors => Set<MovieActor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Movie>()
            .HasOne(movie => movie.Details)
            .WithOne(details => details.Movie)
            .HasForeignKey<MovieDetails>(details => details.MovieId);

        modelBuilder.Entity<Review>(review =>
        {
            review.ToTable(table => table.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
            review.HasOne(item => item.Movie)
                .WithMany(movie => movie.Reviews)
                .HasForeignKey(item => item.MovieId);
        });

        modelBuilder.Entity<Movie>()
            .HasMany(movie => movie.Actors)
            .WithMany(actor => actor.Movies)
            .UsingEntity<MovieActor>(
                actor => actor.HasOne(castMember => castMember.Actor).WithMany()
                    .HasForeignKey(castMember => castMember.ActorId),
                movie => movie.HasOne(castMember => castMember.Movie).WithMany(movie => movie.Cast)
                    .HasForeignKey(castMember => castMember.MovieId),
                join =>
                {
                    join.ToTable("MovieActor");
                    join.HasKey(castMember => new { castMember.MovieId, castMember.ActorId });
                    join.Property(castMember => castMember.Role).HasMaxLength(200);
                });
    }
}
