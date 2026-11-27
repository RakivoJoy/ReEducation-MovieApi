namespace MovieApi.DTOs;

/// <summary>A movie with its optional details, reviews, and cast.</summary>
/// <param name="Id">The identifier of the returned resource.</param>
/// <param name="Title">The movie title.</param>
/// <param name="Year">The release year.</param>
/// <param name="Genre">The movie genre.</param>
/// <param name="Duration">The movie runtime in minutes.</param>
/// <param name="Details">The supplementary details, or null when absent.</param>
/// <param name="Reviews">The reviews for this movie.</param>
/// <param name="Actors">The cast entries for this movie.</param>
public sealed record MovieDetailDto(
    int Id,
    string Title,
    int Year,
    string Genre,
    int Duration,
    MovieDetailsDto? Details,
    List<ReviewDto> Reviews,
    List<MovieCastMemberDto> Actors);
