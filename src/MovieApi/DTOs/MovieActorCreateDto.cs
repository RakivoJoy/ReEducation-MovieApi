using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

public sealed class MovieActorCreateDto
{
    [Required]
    [StringLength(200)]
    public string Role { get; init; } = string.Empty;
}
