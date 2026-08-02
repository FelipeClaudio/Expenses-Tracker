using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class TopicTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task PostTopics_CreatesRootTopicWithCreatorAsFirstMember()
    {
        var client = factory.CreateClient();
        var creator = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var createResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Portugal Trip" });
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.NotNull(topic);
        Assert.Equal("Portugal Trip", topic!.Name);
        Assert.Null(topic.ParentTopicId);
        Assert.Equal(topic.Id, topic.RootTopicId);
        Assert.Equal(creator.Id, topic.CreatedByUserId);
        Assert.False(string.IsNullOrWhiteSpace(topic.InviteCode));

        // Membership proof: FR-3 requires GET to be member-only, so the
        // creator succeeding here proves they were added as a member on
        // creation (UC-2's stated postcondition), not just that the row
        // exists in the DB.
        var getResponse = await client.GetAsync($"/api/topics/{topic.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task PatchTopic_ByAnyMember_RenamesSuccessfully()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var createResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Old Name" });
        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var patchResponse = await client.PatchAsJsonAsync($"/api/topics/{topic!.Id}", new { name = "New Name" });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var renamed = await patchResponse.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.Equal("New Name", renamed!.Name);

        var getResponse = await client.GetAsync($"/api/topics/{topic.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.Equal("New Name", fetched!.Name);
    }
}
