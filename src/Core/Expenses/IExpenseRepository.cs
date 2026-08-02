namespace Core.Expenses;

public interface IExpenseRepository
{
    Task<Expense> AddAsync(Expense expense, IReadOnlyList<ExpenseParticipant> participants, CancellationToken cancellationToken = default);

    Task<Expense?> FindByIdAsync(Guid expenseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseParticipant>> FindParticipantsAsync(Guid expenseId, CancellationToken cancellationToken = default);

    /// <summary>Every expense logged directly at any of the given topic ids, with its participants.</summary>
    Task<IReadOnlyList<ExpenseWithParticipants>> FindByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default);

    Task<int> CountByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default);

    /// <summary>Replaces the expense's fields and its entire participant set (FR-13).</summary>
    Task UpdateAsync(Expense expense, IReadOnlyList<ExpenseParticipant> newParticipants, CancellationToken cancellationToken = default);

    Task DeleteAsync(Expense expense, CancellationToken cancellationToken = default);
}
