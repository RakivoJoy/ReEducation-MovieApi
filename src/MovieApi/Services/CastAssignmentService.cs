using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.Models;

namespace MovieApi.Services;

public enum CastAssignmentResult
{
    Created,
    Updated,
    ResourceMissing,
    RoleAlreadyAssigned
}

public sealed class CastAssignmentService(MovieDbContext context)
{
    private const int SqlServerConstraintViolation = 547;
    private const int SqlServerPrimaryKeyViolation = 2627;

    public async Task<CastAssignmentResult> AssignRoleAsync(
        int movieId, int actorId, string role, CancellationToken cancellationToken)
    {
        if (!await context.Movies.AnyAsync(movie => movie.Id == movieId, cancellationToken) ||
            !await context.Actors.AnyAsync(actor => actor.Id == actorId, cancellationToken))
        {
            return CastAssignmentResult.ResourceMissing;
        }

        var castMember = await context.MovieActors.FindAsync([movieId, actorId], cancellationToken);
        if (castMember?.Role is not null)
        {
            return CastAssignmentResult.RoleAlreadyAssigned;
        }

        if (castMember is null)
        {
            context.MovieActors.Add(new MovieActor { MovieId = movieId, ActorId = actorId, Role = role });
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqlException { Number: SqlServerPrimaryKeyViolation })
            {
                return CastAssignmentResult.RoleAlreadyAssigned;
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqlException { Number: SqlServerConstraintViolation })
            {
                return CastAssignmentResult.ResourceMissing;
            }

            return CastAssignmentResult.Created;
        }

        // The null predicate prevents a concurrent request from overwriting a role.
        var updated = await context.MovieActors
            .Where(member => member.MovieId == movieId && member.ActorId == actorId && member.Role == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(member => member.Role, role), cancellationToken);
        if (updated > 0)
        {
            return CastAssignmentResult.Updated;
        }

        return await context.MovieActors.AnyAsync(
            member => member.MovieId == movieId && member.ActorId == actorId, cancellationToken)
            ? CastAssignmentResult.RoleAlreadyAssigned
            : CastAssignmentResult.ResourceMissing;
    }
}
