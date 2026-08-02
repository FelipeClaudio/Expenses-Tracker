using System.Collections.Concurrent;
using Core.Auth;

namespace Api.IntegrationTests.Fakes;

/// <summary>
/// Deterministic stand-in for real Google ID token validation, so auth
/// tests never call Google's servers. Tests register a token string with a
/// payload via <see cref="Register"/>; any unregistered token is treated as
/// invalid, matching real validator behavior for an expired/malformed token.
/// </summary>
public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    private readonly ConcurrentDictionary<string, GoogleTokenPayload> _tokens = new();

    public string Register(GoogleTokenPayload payload)
    {
        var token = $"fake-token:{Guid.NewGuid()}";
        _tokens[token] = payload;
        return token;
    }

    public Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_tokens.GetValueOrDefault(idToken));
    }
}
