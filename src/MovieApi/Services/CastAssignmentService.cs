using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.Models;

namespace MovieApi.Services;

/// <summary>Describes the outcome of assigning a role to a movie cast member.</summary>
public enum CastAssignmentResult
{
    /// <summary>A new movie-actor link was created with the requested role.</summary>
    Created,
    /// <summary>The role was set on an existing link whose role was null.</summary>
    Updated,
    /// <summary>The movie, actor, or previously observed link no longer exists.</summary>
    ResourceMissing,
    /// <summary>The link already has a role, including when another request assigns it first.</summary>
    RoleAlreadyAssigned
}

/// <summary>Assigns roles to movie cast members while preserving roles already assigned.</summary>
public sealed class CastAssignmentService(MovieDbContext context)
{
    private const int SqlServerConstraintViolation = 547;
    private const int SqlServerPrimaryKeyViolation = 2627;

    /// <summary>Creates a cast link or fills the null role on an existing link.</summary>
    /// <param name="movieId">The movie identifier.</param>
    /// <param name="actorId">The actor identifier.</param>
    /// <param name="role">The role to assign.</param>
    /// <param name="cancellationToken">Cancels the database operations.</param>
    /// <returns>The <see cref="CastAssignmentResult"/> describing the outcome.</returns>
    /// <remarks>Constraint failures account for inserts or deletes that race the initial checks.</remarks>
    public async Task<CastAssignmentResult> AssignRoleAsync(
        int movieId, int actorId, string role, CancellationToken cancellationToken)
    {
        if (!await context.Movies.AnyAsync(movie => movie.Id == movieId, cancellationToken) ||
            !await context.Actors.AnyAsync(actor => actor.Id == actorId, cancellationToken))
        {
            return CastAssignmentResult.ResourceMissing;
        }

        var castMember = await context.MovieActors.FindAsync([movieId, actorId], cancellationToken);
        if (castMember is null)
        {
            return await CreateCastLinkAsync(movieId, actorId, role, cancellationToken);
        }

        if (castMember.Role is not null)
        {
            return CastAssignmentResult.RoleAlreadyAssigned;
        }

        return await FillUnassignedRoleAsync(movieId, actorId, role, cancellationToken);
    }

    private async Task<CastAssignmentResult> CreateCastLinkAsync(
        int movieId, int actorId, string role, CancellationToken cancellationToken)
    {
        context.MovieActors.Add(new MovieActor { MovieId = movieId, ActorId = actorId, Role = role });
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        // Another request can insert the link or delete a parent after the prechecks.
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

    private async Task<CastAssignmentResult> FillUnassignedRoleAsync(
        int movieId, int actorId, string role, CancellationToken cancellationToken)
    {
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
