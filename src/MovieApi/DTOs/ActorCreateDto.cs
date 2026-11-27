using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

public sealed class ActorCreateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(1, 2100)]
    public int BirthYear { get; init; }
}
