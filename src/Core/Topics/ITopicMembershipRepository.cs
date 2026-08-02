namespace Core.Topics;

public interface ITopicMembershipRepository
{
    Task<bool> IsMemberAsync(Guid rootTopicId, Guid userId, CancellationToken cancellationToken = default);

    Task AddMemberAsync(TopicMember member, CancellationToken cancellationToken = default);
}
