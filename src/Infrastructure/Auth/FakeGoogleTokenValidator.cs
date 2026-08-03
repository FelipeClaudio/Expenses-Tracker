using System.Text.Json;
using Core.Auth;

namespace Infrastructure.Auth;

/// <summary>
/// Deterministic stand-in for real Google ID token validation, reachable by
/// a live running process (unlike the WebApplicationFactory-swapped fake
/// Api.IntegrationTests uses). Only ever wired up when Auth:FakeMode is
/// explicitly set - see ServiceCollectionExtensions - so this never runs in
/// production. Treats the "idToken" as a JSON-encoded GoogleTokenPayload
/// instead of a real Google JWT.
/// </summary>
public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<GoogleTokenPayload>(idToken, SerializerOptions);
            return Task.FromResult(payload);
        }
        catch (JsonException)
        {
            return Task.FromResult<GoogleTokenPayload?>(null);
        }
    }
}
