namespace Core.Expenses;

public sealed record ExpenseWithParticipants(Expense Expense, IReadOnlyList<ExpenseParticipant> Participants);
