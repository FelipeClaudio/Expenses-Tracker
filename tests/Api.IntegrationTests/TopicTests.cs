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

    [Fact]
    public async Task PatchTopic_ByNonCreatorMember_RenamesSuccessfully()
    {
        // FR-8 grants renaming to "any Member," not just the creator - this
        // proves a second, non-creator member can actually do it.
        var ownerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var createResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Old Name" });
        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{topic!.InviteCode}", null);

        var patchResponse = await memberClient.PatchAsJsonAsync($"/api/topics/{topic.Id}", new { name = "Renamed By Member" });

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var renamed = await patchResponse.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.Equal("Renamed By Member", renamed!.Name);
    }

    [Fact]
    public async Task GetTopics_ListsOnlyRootTopicsCurrentUserBelongsTo()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var mineResponse = await client.PostAsJsonAsync("/api/topics", new { name = "My Trip" });
        var mine = await mineResponse.Content.ReadFromJsonAsync<TopicResponse>();

        // A subtopic of my own root must NOT show up as a separate entry -
        // the list is root topics only.
        await client.PostAsJsonAsync($"/api/topics/{mine!.Id}/subtopics", new { name = "Fuel" });

        var otherClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(otherClient, factory.GoogleTokenValidator);
        await otherClient.PostAsJsonAsync("/api/topics", new { name = "Someone Else's Trip" });

        var response = await client.GetAsync("/api/topics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var topics = await response.Content.ReadFromJsonAsync<List<TopicResponse>>();
        var listed = Assert.Single(topics!);
        Assert.Equal(mine.Id, listed.Id);
    }

    [Fact]
    public async Task GetById_ForNonexistentTopic_Returns404()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var response = await client.GetAsync($"/api/topics/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostTopics_WithWhitespaceName_Returns400(string name)
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var response = await client.PostAsJsonAsync("/api/topics", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PatchTopic_WithWhitespaceName_Returns400(string name)
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var createResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Valid Name" });
        var topic = await createResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var response = await client.PatchAsJsonAsync($"/api/topics/{topic!.Id}", new { name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
