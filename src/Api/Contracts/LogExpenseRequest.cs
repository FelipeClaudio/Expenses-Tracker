namespace Api.Contracts;

public sealed record LogExpenseRequest(
    string Description,
    decimal Amount,
    Guid PaidByUserId,
    DateTimeOffset ExpenseDate,
    IReadOnlyList<Guid> ParticipantUserIds);
