using Core.Users;

namespace Core.Auth;

public interface IAuthService
{
    /// <summary>
    /// Validates a Google ID token and returns the corresponding user,
    /// creating one on first sign-in (FR-2). Returns null if the token is
    /// invalid (FR-1).
    /// </summary>
    Task<User?> AuthenticateAsync(string googleIdToken, CancellationToken cancellationToken = default);
}
