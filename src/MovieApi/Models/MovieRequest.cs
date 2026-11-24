namespace MovieApi.Models;

public class MovieRequest
{
    public required string Title { get; init; }
    public required int Year { get; init; }
    public required string Genre { get; init; }
    public required int Duration { get; init; }
}
