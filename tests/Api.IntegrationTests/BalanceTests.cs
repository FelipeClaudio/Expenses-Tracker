using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class BalanceTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task GetBalances_AggregatesAcrossAllDescendantSubtopics()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{root!.InviteCode}", null);

        var subtopicResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/subtopics", new { name = "Fuel" });
        var subtopic = await subtopicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        // On the root: owner pays 30, split with member -> owner +15, member -15.
        await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/expenses", new
        {
            description = "Groceries",
            amount = 30.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });

        // On the subtopic: member pays 20, split with owner -> member +10, owner -10.
        await memberClient.PostAsJsonAsync($"/api/topics/{subtopic!.Id}/expenses", new
        {
            description = "Petrol",
            amount = 20.00m,
            paidByUserId = member.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });

        // Combined: owner = 15 - 10 = 5; member = -15 + 10 = -5.
        var rootBalancesResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/balances");
        var rootBalances = (await rootBalancesResponse.Content.ReadFromJsonAsync<List<BalanceResponse>>())!;
        Assert.Equal(5.00m, rootBalances.Single(b => b.UserId == owner.Id).NetBalance);
        Assert.Equal(-5.00m, rootBalances.Single(b => b.UserId == member.Id).NetBalance);

        // Requested from the SUBTOPIC's perspective, balances still
        // aggregate the whole root (spec §3), not just that node.
        var subtopicBalancesResponse = await ownerClient.GetAsync($"/api/topics/{subtopic.Id}/balances");
        var subtopicBalances = (await subtopicBalancesResponse.Content.ReadFromJsonAsync<List<BalanceResponse>>())!;
        Assert.Equal(5.00m, subtopicBalances.Single(b => b.UserId == owner.Id).NetBalance);
        Assert.Equal(-5.00m, subtopicBalances.Single(b => b.UserId == member.Id).NetBalance);
    }

    [Fact]
    public async Task GetBalances_ImmediatelyReflectsNewlyLoggedExpense_NotCachedStale()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{root!.InviteCode}", null);

        await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/expenses", new
        {
            description = "Dinner",
            amount = 20.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });

        var firstReadResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/balances");
        var firstRead = (await firstReadResponse.Content.ReadFromJsonAsync<List<BalanceResponse>>())!;
        Assert.Equal(10.00m, firstRead.Single(b => b.UserId == owner.Id).NetBalance);

        // A second expense logged in the same test, no cache-clearing step
        // anywhere - the very next read must already reflect it (FR-17).
        await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/expenses", new
        {
            description = "Snacks",
            amount = 10.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });

        var secondReadResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/balances");
        var secondRead = (await secondReadResponse.Content.ReadFromJsonAsync<List<BalanceResponse>>())!;
        Assert.Equal(15.00m, secondRead.Single(b => b.UserId == owner.Id).NetBalance);
        Assert.Equal(-15.00m, secondRead.Single(b => b.UserId == member.Id).NetBalance);
    }

    [Fact]
    public async Task GetBalances_ForNonexistentTopic_Returns404()
    {
        var client = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);

        var response = await client.GetAsync($"/api/topics/{Guid.NewGuid()}/balances");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBalances_IncludesMembersWithNoExpenseActivity_AtZero()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        // Joins the topic but never pays for or is tagged on any expense.
        var bystanderClient = factory.CreateClient();
        var bystander = await AuthTestHelpers.SignInAsNewUserAsync(bystanderClient, factory.GoogleTokenValidator);
        await bystanderClient.PostAsync($"/api/topics/join/{root!.InviteCode}", null);

        await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/expenses", new
        {
            description = "Solo dinner",
            amount = 10.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id },
        });

        var balancesResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/balances");
        var balances = (await balancesResponse.Content.ReadFromJsonAsync<List<BalanceResponse>>())!;

        // UC-10: every member of the root Topic must appear, even at 0.00.
        Assert.Equal(0.00m, balances.Single(b => b.UserId == bystander.Id).NetBalance);
    }
}
