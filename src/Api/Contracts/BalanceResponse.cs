namespace Api.Contracts;

public sealed record BalanceResponse(Guid UserId, decimal NetBalance);
