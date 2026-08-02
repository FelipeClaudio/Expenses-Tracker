using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class TopicInviteTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task RotateInviteCode_InvalidatesPreviousLink()
    {
        var ownerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);

        var createResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var originalCode = topic!.InviteCode;

        var rotateResponse = await ownerClient.PostAsync($"/api/topics/{topic.Id}/invite/rotate", null);
        Assert.Equal(HttpStatusCode.OK, rotateResponse.StatusCode);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<RotateInviteCodeResponse>();
        Assert.NotEqual(originalCode, rotated!.InviteCode);

        var outsiderClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(outsiderClient, factory.GoogleTokenValidator);
        var joinWithOldCode = await outsiderClient.PostAsync($"/api/topics/join/{originalCode}", null);
        Assert.Equal(HttpStatusCode.NotFound, joinWithOldCode.StatusCode);
    }

    [Fact]
    public async Task JoinTopic_WithValidCode_AddsMember()
    {
        var ownerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var createResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var joinerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(joinerClient, factory.GoogleTokenValidator);

        var joinResponse = await joinerClient.PostAsync($"/api/topics/join/{topic!.InviteCode}", null);
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        // Membership proof: the joiner can now access the topic (FR-3 would
        // 403 a non-member).
        var getResponse = await joinerClient.GetAsync($"/api/topics/{topic.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task JoinTopic_WithRotatedCode_IsRejected()
    {
        var ownerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var createResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var staleCode = topic!.InviteCode;

        await ownerClient.PostAsync($"/api/topics/{topic.Id}/invite/rotate", null);

        var joinerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(joinerClient, factory.GoogleTokenValidator);
        var joinResponse = await joinerClient.PostAsync($"/api/topics/join/{staleCode}", null);

        Assert.Equal(HttpStatusCode.NotFound, joinResponse.StatusCode);
    }

    [Fact]
    public async Task JoinTopic_WithCodeThatWasNeverValid_Returns404()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var response = await client.PostAsync($"/api/topics/join/{Guid.NewGuid():N}", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RotateInviteCode_OnNonRootTopic_Returns400()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var subtopicResponse = await client.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        var subtopic = await subtopicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var rotateResponse = await client.PostAsync($"/api/topics/{subtopic!.Id}/invite/rotate", null);

        Assert.Equal(HttpStatusCode.BadRequest, rotateResponse.StatusCode);
    }
}
