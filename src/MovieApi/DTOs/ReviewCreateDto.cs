using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

public sealed class ReviewCreateDto
{
    [Required]
    [StringLength(200)]
    public string ReviewerName { get; init; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Comment { get; init; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; init; }
}
