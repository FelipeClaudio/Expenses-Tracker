using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class SettlementApiTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task GetSettlements_ReturnsMinimalTransferList()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{root!.InviteCode}", null);

        var thirdClient = factory.CreateClient();
        var third = await AuthTestHelpers.SignInAsNewUserAsync(thirdClient, factory.GoogleTokenValidator);
        await thirdClient.PostAsync($"/api/topics/join/{root.InviteCode}", null);

        // Owner pays 30 for all three -> owner +20, member -10, third -10.
        await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/expenses", new
        {
            description = "Dinner",
            amount = 30.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id, third.Id },
        });

        var response = await ownerClient.GetAsync($"/api/topics/{root.Id}/settlements");
        var transfers = await response.Content.ReadFromJsonAsync<List<SettlementTransferResponse>>();

        // Minimal for 3 participants: at most n-1 = 2 transfers, both
        // resolving fully into the single creditor (owner).
        Assert.True(transfers!.Count <= 2);
        Assert.All(transfers, t => Assert.Equal(owner.Id, t.ToUserId));
        Assert.Equal(20.00m, transfers.Sum(t => t.Amount));
    }

    [Fact]
    public async Task GetSettlements_ImmediatelyReflectsEditedExpense_NotCachedStale()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{root!.InviteCode}", null);

        var createResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/expenses", new
        {
            description = "Dinner",
            amount = 20.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var firstReadResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/settlements");
        var firstRead = await firstReadResponse.Content.ReadFromJsonAsync<List<SettlementTransferResponse>>();
        Assert.Equal(10.00m, Assert.Single(firstRead!).Amount);

        // Editing (not just logging a new expense) must be reflected
        // immediately too - no cache anywhere in this path (FR-17).
        await ownerClient.PutAsJsonAsync($"/api/expenses/{created!.Id}", new
        {
            description = "Dinner (updated)",
            amount = 40.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });

        var secondReadResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/settlements");
        var secondRead = await secondReadResponse.Content.ReadFromJsonAsync<List<SettlementTransferResponse>>();
        var transfer = Assert.Single(secondRead!);
        Assert.Equal(member.Id, transfer.FromUserId);
        Assert.Equal(owner.Id, transfer.ToUserId);
        Assert.Equal(20.00m, transfer.Amount);
    }
}
