namespace MovieApi.DTOs;

public sealed record ReviewDto(int Id, string ReviewerName, string Comment, int Rating);
