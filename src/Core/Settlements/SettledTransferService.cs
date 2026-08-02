using Core.Auth;

namespace Core.Settlements;

public sealed class SettledTransferService(ISettledTransferRepository repository, IClock clock) : ISettledTransferService
{
    public Task<SettledTransfer> RecordSettlementAsync(
        Guid rootTopicId,
        Guid fromUserId,
        Guid toUserId,
        decimal amount,
        Guid recordedByUserId,
        CancellationToken cancellationToken = default)
    {
        var settledTransfer = new SettledTransfer
        {
            Id = Guid.NewGuid(),
            RootTopicId = rootTopicId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Amount = amount,
            RecordedByUserId = recordedByUserId,
            RecordedAt = clock.UtcNow,
        };

        return repository.AddAsync(settledTransfer, cancellationToken);
    }
}
