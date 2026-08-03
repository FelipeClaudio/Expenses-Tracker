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
    [InlineData("GET", "/api/topics")]
    [InlineData("POST", "/api/topics")]
    [InlineData("GET", "/api/topics/00000000-0000-0000-0000-000000000000")]
    [InlineData("PATCH", "/api/topics/00000000-0000-0000-0000-000000000000")]
    [InlineData("GET", "/api/topics/00000000-0000-0000-0000-000000000000/subtopics")]
    [InlineData("POST", "/api/topics/00000000-0000-0000-0000-000000000000/subtopics")]
    [InlineData("POST", "/api/topics/00000000-0000-0000-0000-000000000000/invite/rotate")]
    [InlineData("POST", "/api/topics/join/some-code")]
    [InlineData("DELETE", "/api/topics/00000000-0000-0000-0000-000000000000")]
    [InlineData("GET", "/api/topics/00000000-0000-0000-0000-000000000000/expenses")]
    [InlineData("POST", "/api/topics/00000000-0000-0000-0000-000000000000/expenses")]
    [InlineData("PUT", "/api/expenses/00000000-0000-0000-0000-000000000000")]
    [InlineData("DELETE", "/api/expenses/00000000-0000-0000-0000-000000000000")]
    [InlineData("GET", "/api/topics/00000000-0000-0000-0000-000000000000/balances")]
    [InlineData("GET", "/api/topics/00000000-0000-0000-0000-000000000000/settlements")]
    [InlineData("POST", "/api/topics/00000000-0000-0000-0000-000000000000/settlements/mark-paid")]
    public async Task NoSessionCookie_Returns401ForAllProtectedEndpoints(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
