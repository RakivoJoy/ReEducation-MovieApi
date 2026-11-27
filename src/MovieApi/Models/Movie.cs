namespace MovieApi.Models;

/// <summary>A catalog entry with optional details, reviews, and cast links.</summary>
public class Movie
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int Year { get; set; }
    public required string Genre { get; set; }
    public int Duration { get; set; }
    public MovieDetails? Details { get; set; }
    public ICollection<Review> Reviews { get; } = new List<Review>();
    public ICollection<Actor> Actors { get; } = new List<Actor>();
    public ICollection<MovieActor> Cast { get; } = new List<MovieActor>();
}
