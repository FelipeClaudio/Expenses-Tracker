namespace Core.Topics;

/// <summary>
/// Membership is granted on a root topic only; every member of a root has
/// equal rights across it and all of its descendant subtopics.
/// </summary>
public sealed class TopicMember
{
    public required Guid RootTopicId { get; init; }

    public required Guid UserId { get; init; }

    public required DateTimeOffset JoinedAt { get; init; }
}
