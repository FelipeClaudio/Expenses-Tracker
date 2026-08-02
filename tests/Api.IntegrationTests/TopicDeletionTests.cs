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
        var client = factory.CreateClient();
        var user = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var midResponse = await client.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        var mid = await midResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var leafResponse = await client.PostAsJsonAsync($"/api/topics/{mid!.Id}/subtopics", new { name = "Receipts" });
        var leaf = await leafResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var expenseResponse = await client.PostAsJsonAsync($"/api/topics/{leaf!.Id}/expenses", new
        {
            description = "Fuel receipt",
            amount = 15.00m,
            paidByUserId = user.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { user.Id },
        });
        var expense = await expenseResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var deleteResponse = await SendDeleteAsync(client, mid.Id, confirmCascade: true);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/topics/{mid.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/topics/{leaf.Id}")).StatusCode);

        // The expense logged on the now-deleted leaf must be gone too -
        // proven via the root's (still-alive) expense listing, since the
        // leaf itself no longer exists to query directly.
        var rootExpensesResponse = await client.GetAsync($"/api/topics/{root.Id}/expenses");
        var rootExpenses = await rootExpensesResponse.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.DoesNotContain(rootExpenses!, e => e.Id == expense!.Id);

        // The root itself, a sibling of the deleted branch, is untouched.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/topics/{root.Id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteTopic_WithExpensesButNoSubtopics_WithoutConfirmation_Returns409WithExpenseCount()
    {
        var client = factory.CreateClient();
        var user = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        await client.PostAsJsonAsync($"/api/topics/{root!.Id}/expenses", new
        {
            description = "Souvenirs",
            amount = 8.00m,
            paidByUserId = user.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { user.Id },
        });

        // Zero subtopics, but a real expense directly on the topic - FR-9
        // requires confirmation for either kind of descendant, not just
        // subtopics.
        var deleteResponse = await SendDeleteAsync(client, root.Id, confirmName: root.Name);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var warning = await deleteResponse.Content.ReadFromJsonAsync<DeleteTopicWarningResponse>();
        Assert.Equal(0, warning!.SubtopicCount);
        Assert.Equal(1, warning.ExpenseCount);
    }

    [Fact]
    public async Task DeleteTopic_DeepCascade_RemovesAllDescendantsAtAnyDepth()
    {
        // Stress-tests the claim that a single SaveChanges correctly orders
        // the DELETE statements for an arbitrarily deep tree via EF Core's
        // own FK-dependency graph, not just the 2-3 levels the other tests
        // use - deletes from the middle of a 5-level chain.
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var root = await CreateAsync(client, "/api/topics", "Root");
        var level1 = await CreateAsync(client, $"/api/topics/{root.Id}/subtopics", "L1");
        var level2 = await CreateAsync(client, $"/api/topics/{level1.Id}/subtopics", "L2");
        var level3 = await CreateAsync(client, $"/api/topics/{level2.Id}/subtopics", "L3");
        var level4 = await CreateAsync(client, $"/api/topics/{level3.Id}/subtopics", "L4");
        var level5 = await CreateAsync(client, $"/api/topics/{level4.Id}/subtopics", "L5");

        var deleteResponse = await SendDeleteAsync(client, level2.Id, confirmCascade: true);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        foreach (var deleted in new[] { level2, level3, level4, level5 })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/topics/{deleted.Id}")).StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/topics/{root.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/topics/{level1.Id}")).StatusCode);

        static async Task<TopicResponse> CreateAsync(HttpClient c, string path, string name)
        {
            var response = await c.PostAsJsonAsync(path, new { name });
            return (await response.Content.ReadFromJsonAsync<TopicResponse>())!;
        }
    }

    [Fact]
    public async Task DeleteTopic_ForNonexistentTopic_Returns404()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var response = await SendDeleteAsync(client, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

        // Deliberately deletes the root itself, with a correct confirmName
        // but no confirmCascade, proving the cascade-warning gate fires
        // first regardless - a correct name confirmation alone isn't enough
        // to bypass it when there's a subtopic still hanging off the root.
        var deleteResponse = await SendDeleteAsync(client, root.Id, confirmName: root.Name);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var warning = await deleteResponse.Content.ReadFromJsonAsync<DeleteTopicWarningResponse>();
        Assert.Equal(1, warning!.SubtopicCount);
        Assert.Equal(0, warning.ExpenseCount);
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
