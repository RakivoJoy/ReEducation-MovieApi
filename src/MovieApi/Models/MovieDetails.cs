namespace MovieApi.Models;

/// <summary>Supplementary information for a movie. Each movie can have at most one details record.</summary>
public class MovieDetails
{
    public int Id { get; set; }
    public required string Synopsis { get; set; }
    public required string Language { get; set; }
    public decimal Budget { get; set; }
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
}
