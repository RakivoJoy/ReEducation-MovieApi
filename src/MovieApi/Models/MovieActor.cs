namespace MovieApi.Models;

/// <summary>Links a movie and an actor, with a role scoped to that movie.</summary>
/// <remarks>The movie and actor identifiers together identify the link.</remarks>
public class MovieActor
{
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;
    public int ActorId { get; set; }
    public Actor Actor { get; set; } = null!;
    /// <summary>The assigned role, or null when the link exists without an assignment.</summary>
    public string? Role { get; set; }
}
