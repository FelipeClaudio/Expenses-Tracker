using Core.Users;

namespace Core.Auth;

public sealed class AuthService(
    IGoogleTokenValidator tokenValidator,
    IUserRepository userRepository,
    IClock clock) : IAuthService
{
    public async Task<User?> AuthenticateAsync(string googleIdToken, CancellationToken cancellationToken = default)
    {
        var payload = await tokenValidator.ValidateAsync(googleIdToken, cancellationToken);
        if (payload is null)
        {
            return null;
        }

        var existingUser = await userRepository.FindByGoogleSubjectIdAsync(payload.Subject, cancellationToken);
        if (existingUser is not null)
        {
            return existingUser;
        }

        var newUser = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = payload.Subject,
            Email = payload.Email,
            DisplayName = payload.Name ?? payload.Email,
            AvatarUrl = payload.Picture,
            CreatedAt = clock.UtcNow,
        };

        await userRepository.AddAsync(newUser, cancellationToken);
        return newUser;
    }
}
