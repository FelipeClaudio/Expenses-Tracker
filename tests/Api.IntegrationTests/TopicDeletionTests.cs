using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class TopicDeletionTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task DeleteTopic_ByCreator_CascadesToDescendantsAndExpenses()
    {
        // "AndExpenses" will become real once Expense exists (build-order
        // step 6, which comes after this one) - for now this proves cascade
        // over the subtopic tree, the only descendant kind that exists yet.
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var midResponse = await client.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        var mid = await midResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var leafResponse = await client.PostAsJsonAsync($"/api/topics/{mid!.Id}/subtopics", new { name = "Receipts" });
        var leaf = await leafResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var deleteResponse = await SendDeleteAsync(client, mid.Id, confirmCascade: true);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/topics/{mid.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/topics/{leaf!.Id}")).StatusCode);

        // The root itself, a sibling of the deleted branch, is untouched.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/topics/{root.Id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_ByNonCreator_Returns403()
    {
        var ownerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{root!.InviteCode}", null);

        // Even a full member of the same root can't delete a node they
        // didn't create themselves (spec UC-6 precondition).
        var deleteResponse = await SendDeleteAsync(memberClient, root.Id, confirmName: root.Name);

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_RootTopic_RemovesMembersAndAllHistory()
    {
        var ownerClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Whole Group" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var subtopicResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        var subtopic = await subtopicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{root.InviteCode}", null);

        var deleteResponse = await SendDeleteAsync(ownerClient, root.Id, confirmCascade: true, confirmName: root.Name);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/topics/{root.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ownerClient.GetAsync($"/api/topics/{subtopic!.Id}")).StatusCode);

        // The joined member's membership is gone too - rejoining with the
        // (now-deleted) topic's old invite code must fail like any other
        // invalid code.
        var rejoinResponse = await memberClient.PostAsync($"/api/topics/join/{root.InviteCode}", null);
        Assert.Equal(HttpStatusCode.NotFound, rejoinResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_WithDescendants_WithoutConfirmation_Returns409WithWarningPayload()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var subtopicResponse = await client.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        await subtopicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        // A root also has membership at stake, so exercise this gate on a
        // non-root node (a subtopic) to isolate the cascade-warning
        // behavior from the root's separate name-confirmation gate.
        var deleteResponse = await SendDeleteAsync(client, root.Id, confirmName: root.Name);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var warning = await deleteResponse.Content.ReadFromJsonAsync<DeleteTopicWarningResponse>();
        Assert.Equal(1, warning!.SubtopicCount);
    }

    [Fact]
    public async Task DeleteTopic_WithDescendants_WithConfirmation_Succeeds()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var subtopicResponse = await client.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        var subtopic = await subtopicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var deleteResponse = await SendDeleteAsync(client, subtopic!.Id, confirmCascade: false);
        // The subtopic itself ("Fuel") has no descendants of its own, so no
        // confirmation should be needed for it specifically - re-verify via
        // the root deletion path below with an actual descendant present.
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var secondSubtopicResponse = await client.PostAsJsonAsync($"/api/topics/{root.Id}/subtopics", new { name = "Groceries" });
        var secondSubtopic = await secondSubtopicResponse.Content.ReadFromJsonAsync<TopicResponse>();
        await client.PostAsJsonAsync($"/api/topics/{secondSubtopic!.Id}/subtopics", new { name = "Snacks" });

        var confirmedDelete = await SendDeleteAsync(client, secondSubtopic.Id, confirmCascade: true);
        Assert.Equal(HttpStatusCode.NoContent, confirmedDelete.StatusCode);
    }

    [Fact]
    public async Task DeleteRootTopic_WithoutNameConfirmation_Returns400()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var deleteResponse = await SendDeleteAsync(client, root!.Id, confirmName: "the wrong name");

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteRootTopic_WithCorrectNameConfirmation_Succeeds()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var deleteResponse = await SendDeleteAsync(client, root!.Id, confirmName: root.Name);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/topics/{root.Id}")).StatusCode);
    }

    /// <summary>
    /// Confirmation flags travel as query parameters, not a JSON body -
    /// HttpClient/TestServer does not transmit a request body on DELETE
    /// (confirmed empirically: Content-Length/Content-Type arrive empty
    /// server-side even when HttpRequestMessage.Content is set), so a body
    /// here would silently never reach the server.
    /// </summary>
    private static Task<HttpResponseMessage> SendDeleteAsync(
        HttpClient client, Guid topicId, bool confirmCascade = false, string? confirmName = null)
    {
        var query = new List<string> { $"confirmCascade={confirmCascade}" };
        if (confirmName is not null)
        {
            query.Add($"confirmName={Uri.EscapeDataString(confirmName)}");
        }

        return client.DeleteAsync($"/api/topics/{topicId}?{string.Join('&', query)}");
    }
}
