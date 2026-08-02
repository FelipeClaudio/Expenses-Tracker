namespace Api.Contracts;

public sealed record ExpenseResponse(
    Guid Id,
    Guid TopicId,
    string Description,
    decimal Amount,
    Guid PaidByUserId,
    DateTimeOffset ExpenseDate,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ExpenseParticipantResponse> Participants);
