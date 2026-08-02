using Core.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> FindByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.SingleOrDefaultAsync(u => u.GoogleSubjectId == googleSubjectId, cancellationToken);
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }
        catch (DbUpdateException ex) when (IsGoogleSubjectIdUniqueViolation(ex))
        {
            // Lost the first-sign-in race: another request already inserted
            // a user for this Google account between our FindByGoogleSubjectIdAsync
            // check and this insert. Detach our losing candidate and return
            // the row that actually won, so every caller ends up agreeing on
            // one User for this Google account.
            dbContext.Entry(user).State = EntityState.Detached;
            return await dbContext.Users.SingleAsync(
                u => u.GoogleSubjectId == user.GoogleSubjectId,
                cancellationToken);
        }
    }

    private static bool IsGoogleSubjectIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
