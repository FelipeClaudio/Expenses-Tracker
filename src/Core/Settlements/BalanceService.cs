using Core.Expenses;

namespace Core.Settlements;

public sealed class BalanceService(
    IExpenseRepository expenseRepository,
    ISettledTransferRepository settledTransferRepository) : IBalanceService
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetNetBalancesAsync(
        Guid rootTopicId, IReadOnlyList<Guid> topicIdsInSubtree, CancellationToken cancellationToken = default)
    {
        var balances = new Dictionary<Guid, decimal>();

        var expenses = await expenseRepository.FindByTopicIdsAsync(topicIdsInSubtree, cancellationToken);
        foreach (var (expense, participants) in expenses)
        {
            Adjust(balances, expense.PaidByUserId, expense.Amount);
            foreach (var participant in participants)
            {
                Adjust(balances, participant.UserId, -participant.ShareAmount);
            }
        }

        var settledTransfers = await settledTransferRepository.FindByRootTopicIdAsync(rootTopicId, cancellationToken);
        foreach (var settled in settledTransfers)
        {
            // FromUserId already paid this amount outside the app, so their
            // negative balance improves; ToUserId already received it, so
            // their positive balance decreases by the same amount - this
            // exactly cancels the debt out of every future settlement plan.
            Adjust(balances, settled.FromUserId, settled.Amount);
            Adjust(balances, settled.ToUserId, -settled.Amount);
        }

        return balances;
    }

    private static void Adjust(Dictionary<Guid, decimal> balances, Guid userId, decimal delta)
    {
        balances[userId] = balances.GetValueOrDefault(userId) + delta;
    }
}
