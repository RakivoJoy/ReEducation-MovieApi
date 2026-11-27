using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

/// <summary>Inputs for posting a review for a movie.</summary>
public sealed class ReviewCreateDto
{
    /// <summary>The required reviewer name, up to 200 characters.</summary>
    [Required]
    [StringLength(200)]
    public string ReviewerName { get; init; } = string.Empty;

    /// <summary>The required review text, up to 4000 characters.</summary>
    [Required]
    [StringLength(4000)]
    public string Comment { get; init; } = string.Empty;

    /// <summary>The rating, from 1 through 5.</summary>
    [Range(1, 5)]
    public int Rating { get; init; }
}
