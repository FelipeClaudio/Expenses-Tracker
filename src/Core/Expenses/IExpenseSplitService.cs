namespace Core.Expenses;

public interface IExpenseSplitService
{
    /// <summary>
    /// Splits an amount equally among the given participants (FR-11).
    /// Shares are computed in whole cents so they always sum back exactly
    /// to the original amount; any remainder cent(s) are allocated
    /// deterministically to the first participants in the given order.
    /// </summary>
    IReadOnlyDictionary<Guid, decimal> SplitEqually(decimal amount, IReadOnlyList<Guid> participantUserIds);
}
