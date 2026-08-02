namespace Api.Contracts;

public sealed record MarkSettlementPaidRequest(Guid FromUserId, Guid ToUserId, decimal Amount);
