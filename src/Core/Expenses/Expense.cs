namespace Core.Expenses;

public sealed class Expense
{
    public required Guid Id { get; init; }

    public required Guid TopicId { get; init; }

    public required string Description { get; set; }

    public required decimal Amount { get; set; }

    public required Guid PaidByUserId { get; set; }

    public required DateTimeOffset ExpenseDate { get; set; }

    /// <summary>The Member who logged this expense - not necessarily the payer. Only this user may delete it (FR-12a).</summary>
    public required Guid CreatedByUserId { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
