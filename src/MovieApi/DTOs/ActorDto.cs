namespace MovieApi.DTOs;

/// <summary>A standalone actor profile.</summary>
/// <param name="Id">The identifier of the returned resource.</param>
/// <param name="Name">The actor's name.</param>
/// <param name="BirthYear">The actor's birth year.</param>
public sealed record ActorDto(int Id, string Name, int BirthYear);
