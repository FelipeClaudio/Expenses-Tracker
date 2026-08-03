using Core.Auth;

namespace Core.Topics;

public sealed class TopicService(
    ITopicRepository topicRepository,
    ITopicMembershipRepository membershipRepository,
    IClock clock) : ITopicService
{
    public async Task<Topic> CreateRootTopicAsync(
        string name, string? description, Guid creatorUserId, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var topic = new Topic
        {
            Id = id,
            ParentTopicId = null,
            RootTopicId = id,
            Name = name,
            Description = description,
            InviteCode = GenerateInviteCode(),
            CreatedByUserId = creatorUserId,
            CreatedAt = clock.UtcNow,
        };

        var created = await topicRepository.AddAsync(topic, cancellationToken);

        await membershipRepository.AddMemberAsync(
            new TopicMember { RootTopicId = created.Id, UserId = creatorUserId, JoinedAt = clock.UtcNow },
            cancellationToken);

        return created;
    }

    public Task<Topic> CreateSubtopicAsync(
        Topic parentTopic, string name, string? description, Guid creatorUserId, CancellationToken cancellationToken = default)
    {
        var topic = new Topic
        {
            Id = Guid.NewGuid(),
            ParentTopicId = parentTopic.Id,
            RootTopicId = parentTopic.RootTopicId,
            Name = name,
            Description = description,
            InviteCode = null,
            CreatedByUserId = creatorUserId,
            CreatedAt = clock.UtcNow,
        };

        return topicRepository.AddAsync(topic, cancellationToken);
    }

    public Task<Topic?> GetByIdAsync(Guid topicId, CancellationToken cancellationToken = default) =>
        topicRepository.FindByIdAsync(topicId, cancellationToken);

    public Task<IReadOnlyList<Topic>> GetSubtopicsAsync(Guid parentTopicId, CancellationToken cancellationToken = default) =>
        topicRepository.FindChildrenAsync(parentTopicId, cancellationToken);

    public async Task<Topic> RenameAsync(Topic topic, string newName, CancellationToken cancellationToken = default)
    {
        topic.Name = newName;
        await topicRepository.UpdateAsync(topic, cancellationToken);
        return topic;
    }

    public async Task<string> RotateInviteCodeAsync(Topic rootTopic, CancellationToken cancellationToken = default)
    {
        var newCode = GenerateInviteCode();
        rootTopic.InviteCode = newCode;
        await topicRepository.UpdateAsync(rootTopic, cancellationToken);
        return newCode;
    }

    public async Task<Topic?> JoinByInviteCodeAsync(string inviteCode, Guid userId, CancellationToken cancellationToken = default)
    {
        var rootTopic = await topicRepository.FindRootByInviteCodeAsync(inviteCode, cancellationToken);
        if (rootTopic is null)
        {
            return null;
        }

        if (!await membershipRepository.IsMemberAsync(rootTopic.Id, userId, cancellationToken))
        {
            await membershipRepository.AddMemberAsync(
                new TopicMember { RootTopicId = rootTopic.Id, UserId = userId, JoinedAt = clock.UtcNow },
                cancellationToken);
        }

        return rootTopic;
    }

    public Task<bool> IsMemberAsync(Topic topic, Guid userId, CancellationToken cancellationToken = default) =>
        membershipRepository.IsMemberAsync(topic.RootTopicId, userId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(Topic topic, CancellationToken cancellationToken = default) =>
        membershipRepository.GetMemberUserIdsAsync(topic.RootTopicId, cancellationToken);

    public Task<IReadOnlyList<Topic>> GetDescendantsAsync(Topic topic, CancellationToken cancellationToken = default) =>
        topicRepository.FindDescendantsAsync(topic, cancellationToken);

    public Task DeleteAsync(Topic topic, IReadOnlyList<Topic> descendants, CancellationToken cancellationToken = default) =>
        topicRepository.DeleteAsync(topic, descendants, cancellationToken);

    private static string GenerateInviteCode() => Guid.NewGuid().ToString("N");
}
