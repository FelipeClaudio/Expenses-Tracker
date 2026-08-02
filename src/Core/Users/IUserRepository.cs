namespace Core.Users;

public interface IUserRepository
{
    Task<User?> FindByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new user. If another request already inserted a user with
    /// the same GoogleSubjectId first (the first-sign-in race), returns that
    /// existing, already-persisted user instead of throwing - callers always
    /// get back the one true row for that Google account.
    /// </summary>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
}
