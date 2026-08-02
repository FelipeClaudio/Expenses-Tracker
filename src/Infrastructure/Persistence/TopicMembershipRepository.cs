using Core.Topics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence;

public sealed class TopicMembershipRepository(AppDbContext dbContext) : ITopicMembershipRepository
{
    public Task<bool> IsMemberAsync(Guid rootTopicId, Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.TopicMembers.AnyAsync(
            m => m.RootTopicId == rootTopicId && m.UserId == userId,
            cancellationToken);
    }

    public async Task AddMemberAsync(TopicMember member, CancellationToken cancellationToken = default)
    {
        dbContext.TopicMembers.Add(member);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateMembershipViolation(ex))
        {
            // Lost the join race: another request already added this exact
            // (RootTopicId, UserId) membership between our IsMemberAsync
            // check and this insert - the user is a member either way, so
            // this is a no-op rather than a failure.
            dbContext.Entry(member).State = EntityState.Detached;
        }
    }

    private static bool IsDuplicateMembershipViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
