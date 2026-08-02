using System.Net.Http.Json;
using Api.Contracts;
using Core.Auth;

namespace Api.IntegrationTests.Fakes;

internal static class AuthTestHelpers
{
    /// <summary>
    /// Signs a fresh, randomly-identified user in on the given client (each
    /// HttpClient from WebApplicationFactory has its own cookie jar, so a
    /// distinct client == a distinct authenticated session for tests that
    /// need multiple simultaneous users).
    /// </summary>
    public static async Task<AuthenticatedUserResponse> SignInAsNewUserAsync(
        HttpClient client, FakeGoogleTokenValidator tokenValidator, string? displayName = null)
    {
        var token = tokenValidator.Register(new GoogleTokenPayload(
            Subject: $"google-subject-{Guid.NewGuid()}",
            Email: $"{Guid.NewGuid()}@example.com",
            Name: displayName ?? "Test User",
            Picture: null));

        var response = await client.PostAsJsonAsync("/api/auth/google", new { idToken = token });
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<AuthenticatedUserResponse>())!;
    }
}
