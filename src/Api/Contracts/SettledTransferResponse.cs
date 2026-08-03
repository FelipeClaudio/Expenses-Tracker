namespace Api.Contracts;

public sealed record SettledTransferResponse(Guid Id, Guid FromUserId, Guid ToUserId, decimal Amount, DateTimeOffset RecordedAt);
