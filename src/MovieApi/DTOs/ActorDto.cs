using System.Text.Json.Serialization;

namespace MovieApi.DTOs;

public sealed record ActorDto(int Id, string Name, int BirthYear)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; init; }
}
