namespace MovieApi.Models;

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
}
