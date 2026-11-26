namespace MovieApi.DTOs;

public sealed record MovieDto(int Id, string Title, int Year, string Genre, int Duration);
