using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

/// <summary>The role to assign to an actor in a movie.</summary>
public sealed class MovieActorCreateDto
{
    /// <summary>The required role, up to 200 characters.</summary>
    [Required]
    [StringLength(200)]
    public string Role { get; init; } = string.Empty;
}
