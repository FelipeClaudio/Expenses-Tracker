using Core.Auth;
using Infrastructure.Auth;

namespace Api.IntegrationTests;

/// <summary>
/// Unit-level coverage for the production-reachable fake validator wired up
/// by Auth:FakeMode (see ServiceCollectionExtensions) - distinct from
/// Fakes.FakeGoogleTokenValidator, which only exists inside
/// CustomWebApplicationFactory's in-process DI swap. No Postgres/collection
/// needed since this class has no infrastructure dependencies of its own.
/// </summary>
public class FakeGoogleTokenValidatorTests
{
    private readonly FakeGoogleTokenValidator _validator = new();

    [Fact]
    public async Task ValidateAsync_WithJsonEncodedPayload_ReturnsMatchingPayload()
    {
        var payload = new GoogleTokenPayload("subject-1", "dana@example.com", "Dana", "https://example.com/dana.png");
        var idToken = System.Text.Json.JsonSerializer.Serialize(payload);

        var result = await _validator.ValidateAsync(idToken);

        Assert.Equal(payload, result);
    }

    [Fact]
    public async Task ValidateAsync_WithMalformedInput_ReturnsNull()
    {
        var result = await _validator.ValidateAsync("not-json");

        Assert.Null(result);
    }
}
