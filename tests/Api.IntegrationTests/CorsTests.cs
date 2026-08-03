using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Api.IntegrationTests;

/// <summary>
/// Confirms the Cors:AllowedOrigins-driven policy in Program.cs actually
/// does what it's configured to (allow a listed origin, deny anything
/// else) - the CORS policy itself had no coverage before this.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class CorsTests(CustomWebApplicationFactory factory)
{
    private const string AllowedOrigin = "http://localhost:5173";

    private HttpClient CreateClientAllowingOrigin(string allowedOrigin) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = allowedOrigin,
                });
            });
        }).CreateClient();

    private static HttpRequestMessage PostGoogleAuthFrom(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/google")
        {
            Content = JsonContent.Create(new { idToken = "not-a-real-token" }),
        };
        request.Headers.Add("Origin", origin);
        return request;
    }

    [Fact]
    public async Task RequestFromAllowedOrigin_ReturnsAccessControlHeaders()
    {
        var client = CreateClientAllowingOrigin(AllowedOrigin);

        var response = await client.SendAsync(PostGoogleAuthFrom(AllowedOrigin));

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var originValues));
        Assert.Equal(AllowedOrigin, Assert.Single(originValues!));

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credentialValues));
        Assert.Equal("true", Assert.Single(credentialValues!));
    }

    [Fact]
    public async Task RequestFromDisallowedOrigin_OmitsAccessControlHeaders()
    {
        var client = CreateClientAllowingOrigin(AllowedOrigin);

        var response = await client.SendAsync(PostGoogleAuthFrom("http://evil.example.com"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
