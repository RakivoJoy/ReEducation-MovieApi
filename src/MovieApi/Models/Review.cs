using System.ComponentModel.DataAnnotations;

namespace MovieApi.Models;

/// <summary>A reviewer comment and rating associated with one movie.</summary>
public class Review
{
    public int Id { get; set; }
    public required string ReviewerName { get; set; }
    public required string Comment { get; set; }

    /// <summary>The rating from 1 through 5, also enforced by the database.</summary>
    [Range(1, 5)]
    public int Rating { get; set; }

    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
}
