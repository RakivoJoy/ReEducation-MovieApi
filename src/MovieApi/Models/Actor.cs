namespace MovieApi.Models;

/// <summary>An actor profile that can appear in multiple movies.</summary>
public class Actor
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int BirthYear { get; set; }
    public ICollection<Movie> Movies { get; } = new List<Movie>();
}
