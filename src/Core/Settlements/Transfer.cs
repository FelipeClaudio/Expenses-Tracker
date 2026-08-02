namespace Core.Settlements;

/// <summary>
/// A single suggested payment: <see cref="FromUserId"/> pays
/// <see cref="ToUserId"/> the given <see cref="Amount"/> to help settle a
/// root Topic's balances.
/// </summary>
public sealed record Transfer(Guid FromUserId, Guid ToUserId, decimal Amount);
