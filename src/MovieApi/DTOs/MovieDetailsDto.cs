namespace MovieApi.DTOs;

/// <summary>Supplementary details associated with a movie.</summary>
/// <param name="Id">The identifier of the supplementary details record.</param>
/// <param name="Synopsis">The movie synopsis.</param>
/// <param name="Language">The language listed for the movie.</param>
/// <param name="Budget">The recorded movie budget.</param>
public sealed record MovieDetailsDto(int Id, string Synopsis, string Language, decimal Budget);
