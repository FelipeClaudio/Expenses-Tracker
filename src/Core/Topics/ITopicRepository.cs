namespace Core.Topics;

public interface ITopicRepository
{
    Task<Topic> AddAsync(Topic topic, CancellationToken cancellationToken = default);

    Task<Topic?> FindByIdAsync(Guid topicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Topic>> FindChildrenAsync(Guid parentTopicId, CancellationToken cancellationToken = default);

    Task<Topic?> FindRootByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default);

    Task UpdateAsync(Topic topic, CancellationToken cancellationToken = default);
}
