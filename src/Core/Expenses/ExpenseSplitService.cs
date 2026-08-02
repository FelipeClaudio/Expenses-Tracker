namespace Core.Expenses;

public sealed class ExpenseSplitService : IExpenseSplitService
{
    public IReadOnlyDictionary<Guid, decimal> SplitEqually(decimal amount, IReadOnlyList<Guid> participantUserIds)
    {
        if (participantUserIds.Count == 0)
        {
            throw new ArgumentException("At least one participant is required.", nameof(participantUserIds));
        }

        // Work in whole cents so shares always sum back exactly to the
        // original amount - decimal division alone (e.g. 10.00m / 3) would
        // produce repeating fractions that don't round-trip cleanly.
        var totalCents = (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        var participantCount = participantUserIds.Count;
        var baseCents = totalCents / participantCount;
        var remainderCents = totalCents % participantCount;

        var shares = new Dictionary<Guid, decimal>();
        for (var i = 0; i < participantUserIds.Count; i++)
        {
            var cents = baseCents + (i < remainderCents ? 1 : 0);
            shares[participantUserIds[i]] = cents / 100m;
        }

        return shares;
    }
}
