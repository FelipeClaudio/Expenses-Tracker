using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;
using Core.Auth;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class AuthTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly FakeGoogleTokenValidator _tokenValidator = factory.GoogleTokenValidator;

    [Fact]
    public async Task PostGoogleAuth_WithValidToken_CreatesUserAndSetsSessionCookie()
    {
        var token = _tokenValidator.Register(new GoogleTokenPayload(
            Subject: $"google-subject-{Guid.NewGuid()}",
            Email: "alice@example.com",
            Name: "Alice",
            Picture: "https://example.com/alice.png"));

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = token });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookieValues));
        var setCookie = Assert.Single(cookieValues!);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostGoogleAuth_WithInvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = "not-a-real-token" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostGoogleAuth_FirstSignIn_CreatesUserKeyedByGoogleSubjectId()
    {
        var subject = $"google-subject-{Guid.NewGuid()}";
        var token = _tokenValidator.Register(new GoogleTokenPayload(subject, "bob@example.com", "Bob", null));

        var response = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = token });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        Assert.NotNull(body);
        Assert.Equal("bob@example.com", body!.Email);
        Assert.Equal("Bob", body.DisplayName);
        Assert.NotEqual(Guid.Empty, body.Id);
    }

    [Fact]
    public async Task PostGoogleAuth_SecondSignInSameGoogleAccount_ReusesExistingUser()
    {
        var subject = $"google-subject-{Guid.NewGuid()}";

        var firstToken = _tokenValidator.Register(new GoogleTokenPayload(subject, "carol@example.com", "Carol", null));
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = firstToken });
        var firstBody = await firstResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        var secondToken = _tokenValidator.Register(new GoogleTokenPayload(subject, "carol@example.com", "Carol", null));
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/google", new { idToken = secondToken });
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<AuthenticatedUserResponse>();

        Assert.Equal(firstBody!.Id, secondBody!.Id);
    }
}
