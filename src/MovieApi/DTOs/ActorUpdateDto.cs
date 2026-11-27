using System.ComponentModel.DataAnnotations;

namespace MovieApi.DTOs;

/// <summary>Replacement fields for an actor profile.</summary>
public sealed class ActorUpdateDto
{
    /// <summary>The required name, up to 200 characters.</summary>
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>The birth year, from 1 through 2100.</summary>
    [Range(1, 2100)]
    public int BirthYear { get; init; }
}
