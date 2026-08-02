namespace Core.Settlements;

/// <summary>
/// Greedy largest-creditor/largest-debtor cash-flow minimization (FR-15).
/// Not a proof of the mathematically minimal transaction count (that's
/// NP-hard in general) — this is the practical, industry-standard
/// heuristic, bounded at n-1 transfers for n participants with a nonzero
/// balance.
/// </summary>
public sealed class SettlementService : ISettlementService
{
    public IReadOnlyList<Transfer> ComputeSettlement(IReadOnlyDictionary<Guid, decimal> netBalances)
    {
        var remaining = netBalances
            .Where(b => b.Value != 0m)
            .ToDictionary(b => b.Key, b => b.Value);

        var transfers = new List<Transfer>();

        while (remaining.Any(b => b.Value > 0) && remaining.Any(b => b.Value < 0))
        {
            var creditor = remaining.Where(b => b.Value > 0).MaxBy(b => b.Value);
            var debtor = remaining.Where(b => b.Value < 0).MinBy(b => b.Value);

            var amount = Math.Min(creditor.Value, -debtor.Value);
            transfers.Add(new Transfer(debtor.Key, creditor.Key, amount));

            SetOrRemove(remaining, creditor.Key, creditor.Value - amount);
            SetOrRemove(remaining, debtor.Key, debtor.Value + amount);
        }

        return transfers;
    }

    private static void SetOrRemove(Dictionary<Guid, decimal> balances, Guid userId, decimal newValue)
    {
        if (newValue == 0m)
        {
            balances.Remove(userId);
        }
        else
        {
            balances[userId] = newValue;
        }
    }
}
