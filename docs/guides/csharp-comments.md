# Write C# comments for DocFX and IntelliSense

Use XML documentation for a public contract. Write a short `summary`, use `param` for meaningful inputs, and explain observable results with `returns`. Use `remarks` for extra context and `see` for related types. Match parameter names exactly. Keep text readable in IntelliSense.

```csharp
/// <summary>Assigns an actor a role in a movie.</summary>
/// <param name="movieId">The movie identifier.</param>
/// <returns>A <see cref="CastAssignmentResult"/> that describes the outcome.</returns>
/// <remarks>An existing cast link can be assigned a role while its role is null.</remarks>
```

Add an inline comment where a reader needs the reason behind a decision. In cast assignment, explain that checking for a null role in the update prevents a racing write from being overwritten. A simple assignment such as `Title = request.Title` needs no comment.
