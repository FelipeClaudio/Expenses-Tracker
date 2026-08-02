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

    public async Task<IReadOnlyList<Topic>> FindDescendantsAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        var topic = await FindByIdAsync(topicId, cancellationToken);
        if (topic is null)
        {
            return [];
        }

        // Every topic under the same root, then walk parent-child links in
        // memory - simpler than a recursive SQL CTE and fast enough at this
        // project's scale (NFR-14: tens of members, low thousands of rows).
        var allInRoot = await dbContext.Topics
            .Where(t => t.RootTopicId == topic.RootTopicId)
            .ToListAsync(cancellationToken);

        var childrenByParent = allInRoot
            .Where(t => t.ParentTopicId.HasValue)
            .ToLookup(t => t.ParentTopicId!.Value);

        var descendants = new List<Topic>();
        var queue = new Queue<Guid>();
        queue.Enqueue(topicId);
        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            foreach (var child in childrenByParent[currentId])
            {
                descendants.Add(child);
                queue.Enqueue(child.Id);
            }
        }

        return descendants;
    }

    public async Task DeleteAsync(Topic topic, IReadOnlyList<Topic> descendants, CancellationToken cancellationToken = default)
    {
        if (topic.IsRoot)
        {
            // Deleting a root removes the whole group's membership too
            // (spec UC-6). Done here, in the same SaveChanges as the topic
            // rows below, so both commit atomically in one transaction -
            // TopicMember rows must go first since they reference Topics
            // via a Restrict (not Cascade) foreign key.
            var members = await dbContext.TopicMembers
                .Where(m => m.RootTopicId == topic.Id)
                .ToListAsync(cancellationToken);
            dbContext.TopicMembers.RemoveRange(members);
        }

        dbContext.Topics.RemoveRange(descendants);
        dbContext.Topics.Remove(topic);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
