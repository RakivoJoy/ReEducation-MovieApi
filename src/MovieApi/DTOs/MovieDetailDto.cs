namespace MovieApi.DTOs;

public sealed record MovieDetailDto(
    int Id,
    string Title,
    int Year,
    string Genre,
    int Duration,
    MovieDetailsDto? Details,
    List<ReviewDto> Reviews,
    List<ActorDto> Actors);
