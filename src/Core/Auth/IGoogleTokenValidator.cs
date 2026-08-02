namespace Core.Auth;

/// <summary>The claims Google's Identity Services vouches for once an ID token has been validated.</summary>
public sealed record GoogleTokenPayload(string Subject, string Email, string? Name, string? Picture);

public interface IGoogleTokenValidator
{
    /// <summary>Returns the validated payload, or null if the token is missing, expired, or otherwise invalid.</summary>
    Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
