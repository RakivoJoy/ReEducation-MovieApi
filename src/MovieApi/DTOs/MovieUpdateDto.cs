using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

public sealed class MovieUpdateDto
{
    [Required]
    [StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [Range(1888, 2100)]
    public int Year { get; init; }

    [Required]
    [StringLength(100)]
    public string Genre { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int Duration { get; init; }
}
