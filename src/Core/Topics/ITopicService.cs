namespace Core.Topics;

public interface ITopicService
{
    /// <summary>Creates a new root topic (UC-2/FR-6), adding the creator as its first member.</summary>
    Task<Topic> CreateRootTopicAsync(string name, string? description, Guid creatorUserId, CancellationToken cancellationToken = default);

    /// <summary>Creates a child under any existing, already-validated topic node (UC-5/FR-7).</summary>
    Task<Topic> CreateSubtopicAsync(Topic parentTopic, string name, string? description, Guid creatorUserId, CancellationToken cancellationToken = default);

    Task<Topic?> GetByIdAsync(Guid topicId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Topic>> GetSubtopicsAsync(Guid parentTopicId, CancellationToken cancellationToken = default);

    /// <summary>Renames an already-validated topic node (FR-8).</summary>
    Task<Topic> RenameAsync(Topic topic, string newName, CancellationToken cancellationToken = default);

    /// <summary>Rotates an already-validated root topic's invite code, invalidating the previous one (FR-4).</summary>
    Task<string> RotateInviteCodeAsync(Topic rootTopic, CancellationToken cancellationToken = default);

    /// <summary>Redeems an invite code, adding the user as a member if not already one (FR-5). Null if the code doesn't match any root topic.</summary>
    Task<Topic?> JoinByInviteCodeAsync(string inviteCode, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Whether the user is a member of the given (already-fetched) topic's root (FR-3).</summary>
    Task<bool> IsMemberAsync(Topic topic, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Every member of the given (already-fetched) topic's root (UC-10).</summary>
    Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>Every descendant at any depth under an already-validated topic node (UC-6).</summary>
    Task<IReadOnlyList<Topic>> GetDescendantsAsync(Topic topic, CancellationToken cancellationToken = default);

    /// <summary>Deletes an already-validated topic node and its descendants (UC-6/FR-8a).</summary>
    Task DeleteAsync(Topic topic, IReadOnlyList<Topic> descendants, CancellationToken cancellationToken = default);
}
