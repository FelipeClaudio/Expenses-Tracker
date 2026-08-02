namespace Core.Topics;

/// <summary>
/// One node in the self-referencing Topic tree (spec §3) - a root topic
/// (ParentTopicId null) is the container that owns membership and the
/// invite link; any topic, root or nested, can have child topics under it.
/// </summary>
public sealed class Topic
{
    public required Guid Id { get; init; }

    public Guid? ParentTopicId { get; init; }

    /// <summary>
    /// Denormalized: equal to <see cref="Id"/> for a root topic, otherwise
    /// copied from the parent - lets membership/authorization checks resolve
    /// "which root does this node belong to" without walking the parent
    /// chain.
    /// </summary>
    public required Guid RootTopicId { get; init; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Set only when this is a root topic (ParentTopicId is null).</summary>
    public string? InviteCode { get; set; }

    public required Guid CreatedByUserId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public bool IsRoot => ParentTopicId is null;
}
