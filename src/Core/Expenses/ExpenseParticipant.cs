namespace Core.Expenses;

/// <summary>
/// One tagged participant's share of an expense. ShareAmount is computed
/// and persisted at creation time (or recomputed wholesale on edit) and
/// never drifts afterward (NFR-13) - later membership changes never
/// retroactively alter it.
/// </summary>
public sealed class ExpenseParticipant
{
    public required Guid ExpenseId { get; init; }

    public required Guid UserId { get; init; }

    public required decimal ShareAmount { get; init; }
}
