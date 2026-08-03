namespace Core.Settlements;

public interface ISettledTransferRepository
{
    Task<SettledTransfer> AddAsync(SettledTransfer settledTransfer, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SettledTransfer>> FindByRootTopicIdAsync(Guid rootTopicId, CancellationToken cancellationToken = default);
}
