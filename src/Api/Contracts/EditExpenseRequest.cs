namespace Api.Contracts;

public sealed record EditExpenseRequest(
    string Description,
    decimal Amount,
    Guid PaidByUserId,
    DateTimeOffset ExpenseDate,
    IReadOnlyList<Guid> ParticipantUserIds);
