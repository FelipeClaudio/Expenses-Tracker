using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class SubtopicTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task PostSubtopic_UnderAnyNode_CreatesChildAtUnlimitedDepth()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Portugal Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var level1Response = await client.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        Assert.Equal(HttpStatusCode.OK, level1Response.StatusCode);
        var level1 = await level1Response.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.Equal(root.Id, level1!.ParentTopicId);
        Assert.Equal(root.Id, level1.RootTopicId);
        Assert.Null(level1.InviteCode);

        var level2Response = await client.PostAsJsonAsync($"/api/topics/{level1.Id}/subtopics", new { name = "Rental Car" });
        var level2 = await level2Response.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.Equal(level1.Id, level2!.ParentTopicId);
        Assert.Equal(root.Id, level2.RootTopicId);

        // A third level proves depth isn't artificially capped at 2.
        var level3Response = await client.PostAsJsonAsync($"/api/topics/{level2.Id}/subtopics", new { name = "Insurance" });
        Assert.Equal(HttpStatusCode.OK, level3Response.StatusCode);
        var level3 = await level3Response.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.Equal(level2.Id, level3!.ParentTopicId);
        Assert.Equal(root.Id, level3.RootTopicId);

        var childrenOfRoot = await (await client.GetAsync($"/api/topics/{root.Id}/subtopics"))
            .Content.ReadFromJsonAsync<List<TopicResponse>>();
        Assert.Contains(childrenOfRoot!, t => t.Id == level1.Id);
    }
}
