namespace Core.Settlements;

public interface ISettledTransferService
{
    /// <summary>Records a transfer as settled (UC-12/FR-16), permanently adjusting future balance calculations for this root.</summary>
    Task<SettledTransfer> RecordSettlementAsync(
        Guid rootTopicId,
        Guid fromUserId,
        Guid toUserId,
        decimal amount,
        Guid recordedByUserId,
        CancellationToken cancellationToken = default);
}
