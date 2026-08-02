using System.Net;

namespace Api.IntegrationTests;

/// <summary>
/// NFR-12's authentication half: a request with no session at all must be
/// rejected, independent of AuthorizationTests' membership checks (added
/// alongside the Topic slice). Extend this theory with one more
/// [InlineData] per protected endpoint as later slices add them.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class AuthenticationTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("GET", "/api/users/me")]
    public async Task NoSessionCookie_Returns401ForAllProtectedEndpoints(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
