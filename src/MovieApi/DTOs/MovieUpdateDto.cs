using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

/// <summary>Replacement catalog fields for a movie.</summary>
public sealed class MovieUpdateDto
{
    /// <summary>The required title, up to 200 characters.</summary>
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    /// <summary>The release year, from 1888 through 2100.</summary>
    [Range(1888, 2100)]
    public int Year { get; init; }

    /// <summary>The required genre, up to 100 characters.</summary>
    [Required]
    [StringLength(100)]
    public string Genre { get; init; } = string.Empty;

    /// <summary>The runtime, from 1 through 1440 minutes.</summary>
    [Range(1, 1440)]
    public int Duration { get; init; }
}
