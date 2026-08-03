using Core.Settlements;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class SettledTransferRepository(AppDbContext dbContext) : ISettledTransferRepository
{
    public async Task<SettledTransfer> AddAsync(SettledTransfer settledTransfer, CancellationToken cancellationToken = default)
    {
        dbContext.SettledTransfers.Add(settledTransfer);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settledTransfer;
    }

    public async Task<IReadOnlyList<SettledTransfer>> FindByRootTopicIdAsync(Guid rootTopicId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SettledTransfers
            .Where(s => s.RootTopicId == rootTopicId)
            .ToListAsync(cancellationToken);
    }
}
