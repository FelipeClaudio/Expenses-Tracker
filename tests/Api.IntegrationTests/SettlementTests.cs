using System.Net;
using System.Net.Http.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class SettlementTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task PostMarkPaid_ExcludesTransferFromFutureRecomputation()
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
            amount = 50.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });

        var beforeResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/settlements");
        var before = await beforeResponse.Content.ReadFromJsonAsync<List<SettlementTransferResponse>>();
        var suggested = Assert.Single(before!);
        Assert.Equal(member.Id, suggested.FromUserId);
        Assert.Equal(owner.Id, suggested.ToUserId);
        Assert.Equal(25.00m, suggested.Amount);

        var markPaidResponse = await memberClient.PostAsJsonAsync($"/api/topics/{root.Id}/settlements/mark-paid", new
        {
            fromUserId = member.Id,
            toUserId = owner.Id,
            amount = 25.00m,
        });
        Assert.Equal(HttpStatusCode.OK, markPaidResponse.StatusCode);

        var afterResponse = await ownerClient.GetAsync($"/api/topics/{root.Id}/settlements");
        var after = await afterResponse.Content.ReadFromJsonAsync<List<SettlementTransferResponse>>();
        Assert.Empty(after!);
    }

    [Fact]
    public async Task PostMarkPaid_WithNonPositiveAmount_Returns400()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{root!.InviteCode}", null);

        var response = await ownerClient.PostAsJsonAsync($"/api/topics/{root.Id}/settlements/mark-paid", new
        {
            fromUserId = member.Id,
            toUserId = owner.Id,
            amount = 0.00m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostMarkPaid_WithNonMemberParty_Returns400()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var rootResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var outsiderClient = factory.CreateClient();
        var outsider = await AuthTestHelpers.SignInAsNewUserAsync(outsiderClient, factory.GoogleTokenValidator);

        var response = await ownerClient.PostAsJsonAsync($"/api/topics/{root!.Id}/settlements/mark-paid", new
        {
            fromUserId = outsider.Id,
            toUserId = owner.Id,
            amount = 10.00m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
