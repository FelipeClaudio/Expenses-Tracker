using Core.Auth;
using Core.Topics;

namespace Core.Expenses;

public sealed class ExpenseService(
    IExpenseRepository expenseRepository,
    IExpenseSplitService splitService,
    IClock clock) : IExpenseService
{
    public async Task<ExpenseWithParticipants> LogExpenseAsync(
        Topic topic,
        string description,
        decimal amount,
        Guid paidByUserId,
        DateTimeOffset expenseDate,
        IReadOnlyList<Guid> participantUserIds,
        Guid creatorUserId,
        CancellationToken cancellationToken = default)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            TopicId = topic.Id,
            Description = description,
            Amount = amount,
            PaidByUserId = paidByUserId,
            ExpenseDate = expenseDate,
            CreatedByUserId = creatorUserId,
            CreatedAt = clock.UtcNow,
        };

        var participants = BuildParticipants(expense.Id, amount, participantUserIds);

        var created = await expenseRepository.AddAsync(expense, participants, cancellationToken);
        return new ExpenseWithParticipants(created, participants);
    }

    public Task<Expense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default) =>
        expenseRepository.FindByIdAsync(expenseId, cancellationToken);

    public Task<IReadOnlyList<ExpenseWithParticipants>> GetByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default) =>
        expenseRepository.FindByTopicIdsAsync(topicIds, cancellationToken);

    public Task<int> CountByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default) =>
        expenseRepository.CountByTopicIdsAsync(topicIds, cancellationToken);

    public async Task<ExpenseWithParticipants> EditExpenseAsync(
        Expense expense,
        string description,
        decimal amount,
        Guid paidByUserId,
        DateTimeOffset expenseDate,
        IReadOnlyList<Guid> participantUserIds,
        CancellationToken cancellationToken = default)
    {
        expense.Description = description;
        expense.Amount = amount;
        expense.PaidByUserId = paidByUserId;
        expense.ExpenseDate = expenseDate;

        var newParticipants = BuildParticipants(expense.Id, amount, participantUserIds);

        await expenseRepository.UpdateAsync(expense, newParticipants, cancellationToken);
        return new ExpenseWithParticipants(expense, newParticipants);
    }

    public Task DeleteAsync(Expense expense, CancellationToken cancellationToken = default) =>
        expenseRepository.DeleteAsync(expense, cancellationToken);

    private List<ExpenseParticipant> BuildParticipants(Guid expenseId, decimal amount, IReadOnlyList<Guid> participantUserIds)
    {
        // ExpenseParticipant's key is (ExpenseId, UserId) - a duplicated id
        // in the input must collapse to one participant, not one row per
        // occurrence (which would violate that key at the database level).
        var distinctParticipantUserIds = participantUserIds.Distinct().ToList();
        var shares = splitService.SplitEqually(amount, distinctParticipantUserIds);
        return distinctParticipantUserIds
            .Select(userId => new ExpenseParticipant { ExpenseId = expenseId, UserId = userId, ShareAmount = shares[userId] })
            .ToList();
    }
}
