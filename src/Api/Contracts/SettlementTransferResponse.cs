namespace Api.Contracts;

public sealed record SettlementTransferResponse(Guid FromUserId, Guid ToUserId, decimal Amount);
