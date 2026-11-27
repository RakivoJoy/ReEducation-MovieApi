namespace MovieApi.DTOs;

/// <summary>A movie catalog summary.</summary>
/// <param name="Id">The identifier of the returned resource.</param>
/// <param name="Title">The movie title.</param>
/// <param name="Year">The release year.</param>
/// <param name="Genre">The movie genre.</param>
/// <param name="Duration">The movie runtime in minutes.</param>
public sealed record MovieDto(int Id, string Title, int Year, string Genre, int Duration);
