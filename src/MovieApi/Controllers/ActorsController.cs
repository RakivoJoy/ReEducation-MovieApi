using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.DTOs;
using MovieApi.Models;

namespace MovieApi.Controllers;

[ApiController]
[Route("api/actors")]
public class ActorsController(MovieDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ActorDto>>> GetActors(CancellationToken cancellationToken)
    {
        return await context.Actors.OrderBy(actor => actor.Id)
            .Select(actor => new ActorDto(actor.Id, actor.Name, actor.BirthYear))
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActorDto>> GetActor(int id, CancellationToken cancellationToken)
    {
        var actor = await context.Actors.Where(actor => actor.Id == id)
            .Select(actor => new ActorDto(actor.Id, actor.Name, actor.BirthYear))
            .SingleOrDefaultAsync(cancellationToken);

        return actor is null ? NotFound() : actor;
    }

    [HttpPost]
    public async Task<ActionResult<ActorDto>> PostActor(ActorWriteDto request, CancellationToken cancellationToken)
    {
        var actor = new Actor { Name = request.Name, BirthYear = request.BirthYear };
        context.Actors.Add(actor);
        await context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetActor), new { id = actor.Id }, new ActorDto(actor.Id, actor.Name, actor.BirthYear));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutActor(int id, ActorWriteDto request, CancellationToken cancellationToken)
    {
        var updated = await context.Actors.Where(actor => actor.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(actor => actor.Name, request.Name)
                .SetProperty(actor => actor.BirthYear, request.BirthYear), cancellationToken);

        return updated == 0 ? NotFound() : NoContent();
    }
}
