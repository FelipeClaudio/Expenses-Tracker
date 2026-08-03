namespace Core.Settlements;

public interface IBalanceService
{
    /// <summary>
    /// Each member's net balance (positive = owed money, negative = owes
    /// money) for a root Topic, aggregating every expense across the given
    /// topic ids (the root plus all its descendants, spec §3) and adjusted
    /// by every settlement already recorded for that root (FR-14/FR-16).
    /// Every id in <paramref name="memberUserIds"/> is present in the result
    /// (at 0.00 if they have no expense/settlement activity) - UC-10 shows a
    /// balance for every member of the root Topic, not just active ones.
    /// Always recomputed live - never cached (FR-17).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetNetBalancesAsync(
        Guid rootTopicId,
        IReadOnlyList<Guid> topicIdsInSubtree,
        IReadOnlyList<Guid> memberUserIds,
        CancellationToken cancellationToken = default);
}
