namespace Core.Settlements;

/// <summary>
/// A payment a Member recorded as having actually happened outside the app
/// (UC-12/FR-16) - unlike <see cref="Transfer"/> (an ephemeral, computed
/// suggestion), this is persisted and permanently adjusts future balance
/// calculations for its root Topic.
/// </summary>
public sealed class SettledTransfer
{
    public required Guid Id { get; init; }

    public required Guid RootTopicId { get; init; }

    public required Guid FromUserId { get; init; }

    public required Guid ToUserId { get; init; }

    public required decimal Amount { get; init; }

    public required Guid RecordedByUserId { get; init; }

    public required DateTimeOffset RecordedAt { get; init; }
}
