using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;

namespace MovieApi.Controllers;

/// <summary>Exposes standalone actor profile endpoints.</summary>
[ApiController]
[Route("api/actors")]
public class ActorsController(MovieDbContext context) : ControllerBase
{
    /// <summary>Lists standalone actor profiles ordered by identifier.</summary>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>The actor profiles, or an empty list.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors(CancellationToken cancellationToken)
    {
        return await context.Actors.OrderBy(actor => actor.Id)
            .Select(actor => new ActorDto(actor.Id, actor.Name, actor.BirthYear))
            .ToListAsync(cancellationToken);
    }

    /// <summary>Reads a standalone actor profile.</summary>
    /// <param name="id">The actor identifier.</param>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>The actor profile or HTTP 404.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id, CancellationToken cancellationToken)
    {
        var actor = await context.Actors.Where(actor => actor.Id == id)
            .Select(actor => new ActorDto(actor.Id, actor.Name, actor.BirthYear))
            .SingleOrDefaultAsync(cancellationToken);

        return actor is null ? NotFound() : actor;
    }

    /// <summary>Creates an actor profile.</summary>
    /// <param name="request">The values for the new actor.</param>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>HTTP 201 with the profile and its resource location.</returns>
    [HttpPost]
    public async Task<ActionResult<ActorDto>> PostActor(ActorCreateDto request, CancellationToken cancellationToken)
    {
        var actor = new Actor { Name = request.Name, BirthYear = request.BirthYear };
        context.Actors.Add(actor);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetActor), new { id = actor.Id }, new ActorDto(actor.Id, actor.Name, actor.BirthYear));
    }

    /// <summary>Replaces the name and birth year of an actor.</summary>
    /// <param name="id">The actor identifier.</param>
    /// <param name="request">The replacement profile fields.</param>
    /// <param name="cancellationToken">Cancels work for the request.</param>
    /// <returns>HTTP 204 on success or 404 if the actor is missing.</returns>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutActor(int id, ActorUpdateDto request, CancellationToken cancellationToken)
    {
        var updated = await context.Actors.Where(actor => actor.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(actor => actor.Name, request.Name)
                .SetProperty(actor => actor.BirthYear, request.BirthYear), cancellationToken);

        return updated == 0 ? NotFound() : NoContent();
    }
}
