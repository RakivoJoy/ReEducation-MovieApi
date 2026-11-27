using System.Text.Json.Serialization;

namespace MovieApi.DTOs;

/// <summary>An actor profile in the context of one movie's cast.</summary>
/// <param name="Id">The actor identifier.</param>
/// <param name="Name">The actor's name.</param>
/// <param name="BirthYear">The actor's birth year.</param>
public sealed record MovieCastMemberDto(int Id, string Name, int BirthYear)
{
    /// <summary>The role for this movie, omitted from JSON when unassigned.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; init; }
}
