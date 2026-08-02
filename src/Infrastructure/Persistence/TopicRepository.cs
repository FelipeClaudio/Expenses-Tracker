using Core.Topics;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class TopicRepository(AppDbContext dbContext) : ITopicRepository
{
    public async Task<Topic> AddAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        dbContext.Topics.Add(topic);
        await dbContext.SaveChangesAsync(cancellationToken);
        return topic;
    }

    public Task<Topic?> FindByIdAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        return dbContext.Topics.SingleOrDefaultAsync(t => t.Id == topicId, cancellationToken);
    }

    public async Task<IReadOnlyList<Topic>> FindChildrenAsync(Guid parentTopicId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Topics
            .Where(t => t.ParentTopicId == parentTopicId)
            .ToListAsync(cancellationToken);
    }

    public Task<Topic?> FindRootByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default)
    {
        return dbContext.Topics.SingleOrDefaultAsync(t => t.InviteCode == inviteCode, cancellationToken);
    }

    public async Task UpdateAsync(Topic topic, CancellationToken cancellationToken = default)
    {
        dbContext.Topics.Update(topic);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
