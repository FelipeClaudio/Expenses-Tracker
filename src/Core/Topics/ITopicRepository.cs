namespace Core.Topics;

public interface ITopicRepository
{
    Task<Topic> AddAsync(Topic topic, CancellationToken cancellationToken = default);

    Task<Topic?> FindByIdAsync(Guid topicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Topic>> FindChildrenAsync(Guid parentTopicId, CancellationToken cancellationToken = default);

    Task<Topic?> FindRootByInviteCodeAsync(string inviteCode, CancellationToken cancellationToken = default);

    Task UpdateAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>Every descendant at any depth under the given, already-fetched node (not just direct children).</summary>
    Task<IReadOnlyList<Topic>> FindDescendantsAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the topic and its already-fetched descendants (UC-6/FR-8a).
    /// If <paramref name="topic"/> is a root, also removes that root's
    /// membership (UC-6: deleting a root removes the whole group).
    /// </summary>
    Task DeleteAsync(Topic topic, IReadOnlyList<Topic> descendants, CancellationToken cancellationToken = default);
}
