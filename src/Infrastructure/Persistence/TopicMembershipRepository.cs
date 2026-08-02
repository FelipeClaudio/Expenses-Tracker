using Core.Topics;
using Microsoft.EntityFrameworkCore;

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
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
