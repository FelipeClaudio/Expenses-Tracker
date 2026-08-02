using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

/// <summary>
/// NFR-12's authorization half: an authenticated session that isn't a
/// member of the target Topic's root must be rejected (FR-3), independent
/// of AuthenticationTests' no-session-at-all checks. Extend this theory
/// with one more case per new Topic/Expense endpoint as later slices add
/// them.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class AuthorizationTests(CustomWebApplicationFactory factory)
{
    [Theory]
    [InlineData("GET", "")]
    [InlineData("PATCH", "")]
    [InlineData("GET", "/subtopics")]
    [InlineData("POST", "/subtopics")]
    [InlineData("POST", "/invite/rotate")]
    public async Task NonMember_Returns403ForTopicEndpoints(string method, string subPath)
    {
        var ownerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var createResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Owner's Topic" });
        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var outsiderClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(outsiderClient, factory.GoogleTokenValidator);

        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/topics/{topic!.Id}{subPath}");
        if (method is "PATCH" or "POST" && subPath is "" or "/subtopics")
        {
            request.Content = JsonContent.Create(new { name = "Attempted Change" });
        }

        var response = await outsiderClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
