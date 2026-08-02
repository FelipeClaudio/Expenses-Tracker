namespace Core.Settlements;

public interface IBalanceService
{
    /// <summary>
    /// Each member's net balance (positive = owed money, negative = owes
    /// money) for a root Topic, aggregating every expense across the given
    /// topic ids (the root plus all its descendants, spec §3) and adjusted
    /// by every settlement already recorded for that root (FR-14/FR-16).
    /// Always recomputed live - never cached (FR-17).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetNetBalancesAsync(
        Guid rootTopicId, IReadOnlyList<Guid> topicIdsInSubtree, CancellationToken cancellationToken = default);
}
