namespace MovieApi.DTOs;

/// <summary>A review returned by the API.</summary>
/// <param name="Id">The identifier of the returned resource.</param>
/// <param name="ReviewerName">The name provided by the reviewer.</param>
/// <param name="Comment">The review text.</param>
/// <param name="Rating">The rating from 1 through 5.</param>
public sealed record ReviewDto(int Id, string ReviewerName, string Comment, int Rating);
