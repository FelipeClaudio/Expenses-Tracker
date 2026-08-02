using Core.Topics;

namespace Core.Expenses;

public interface IExpenseService
{
    /// <summary>Logs a new expense against an already-validated topic node (UC-7/FR-10/FR-11).</summary>
    Task<ExpenseWithParticipants> LogExpenseAsync(
        Topic topic,
        string description,
        decimal amount,
        Guid paidByUserId,
        DateTimeOffset expenseDate,
        IReadOnlyList<Guid> participantUserIds,
        Guid creatorUserId,
        CancellationToken cancellationToken = default);

    Task<Expense?> GetByIdAsync(Guid expenseId, CancellationToken cancellationToken = default);

    /// <summary>Every expense logged at any of the given topic ids (e.g. a topic plus all its descendants for UC-13).</summary>
    Task<IReadOnlyList<ExpenseWithParticipants>> GetByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default);

    Task<int> CountByTopicIdsAsync(IReadOnlyList<Guid> topicIds, CancellationToken cancellationToken = default);

    /// <summary>Edits an already-validated expense, recomputing its participant shares from scratch (UC-8/FR-12/FR-13).</summary>
    Task<ExpenseWithParticipants> EditExpenseAsync(
        Expense expense,
        string description,
        decimal amount,
        Guid paidByUserId,
        DateTimeOffset expenseDate,
        IReadOnlyList<Guid> participantUserIds,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes an already-validated expense (UC-9/FR-12a).</summary>
    Task DeleteAsync(Expense expense, CancellationToken cancellationToken = default);
}
