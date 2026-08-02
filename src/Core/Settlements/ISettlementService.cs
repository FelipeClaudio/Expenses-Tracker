namespace Core.Settlements;

public interface ISettlementService
{
    /// <summary>
    /// Given each member's net balance for a root Topic (positive = owed
    /// money, negative = owes money, keyed by user id), computes the minimal
    /// set of transfers that brings every balance to zero.
    /// </summary>
    IReadOnlyList<Transfer> ComputeSettlement(IReadOnlyDictionary<Guid, decimal> netBalances);
}
