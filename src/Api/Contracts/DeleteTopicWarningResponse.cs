namespace Api.Contracts;

/// <summary>ExpenseCount is always 0 until Expense exists (build-order step 6) - included now so the response shape won't need to change later.</summary>
public sealed record DeleteTopicWarningResponse(int SubtopicCount, int ExpenseCount);
