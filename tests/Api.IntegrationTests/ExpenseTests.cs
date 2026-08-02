using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Api.Contracts;
using Api.IntegrationTests.Fakes;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class ExpenseTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task PostExpense_SplitsAmountEquallyAmongTaggedParticipants()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var topicResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{topic!.InviteCode}", null);

        var response = await ownerClient.PostAsJsonAsync($"/api/topics/{topic.Id}/expenses", new
        {
            description = "Dinner",
            amount = 30.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.NotNull(expense);
        Assert.Equal(30.00m, expense!.Amount);
        Assert.Equal(2, expense.Participants.Count);
        Assert.Equal(15.00m, expense.Participants.Single(p => p.UserId == owner.Id).ShareAmount);
        Assert.Equal(15.00m, expense.Participants.Single(p => p.UserId == member.Id).ShareAmount);
    }

    [Fact]
    public async Task PutExpense_ByAnyMember_RecomputesParticipantShares()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var topicResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{topic!.InviteCode}", null);

        var thirdClient = factory.CreateClient();
        var third = await AuthTestHelpers.SignInAsNewUserAsync(thirdClient, factory.GoogleTokenValidator);
        await thirdClient.PostAsync($"/api/topics/join/{topic.InviteCode}", null);

        var createResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{topic.Id}/expenses", new
        {
            description = "Dinner",
            amount = 20.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        // Edited by "member" - not the creator, not even a participant on
        // the original - proving FR-12's "any Member," not creator-only.
        var editResponse = await memberClient.PutAsJsonAsync($"/api/expenses/{created!.Id}", new
        {
            description = "Dinner (updated)",
            amount = 30.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id, third.Id },
        });

        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
        var edited = await editResponse.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.Equal(30.00m, edited!.Amount);
        Assert.Equal(3, edited.Participants.Count);
        Assert.All(edited.Participants, p => Assert.Equal(10.00m, p.ShareAmount));
    }

    [Fact]
    public async Task DeleteExpense_ByCreator_Succeeds()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var topicResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var createResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{topic!.Id}/expenses", new
        {
            description = "Snacks",
            amount = 5.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var deleteResponse = await ownerClient.DeleteAsync($"/api/expenses/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var listResponse = await ownerClient.GetAsync($"/api/topics/{topic.Id}/expenses");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        Assert.DoesNotContain(list!, e => e.Id == created.Id);
    }

    [Fact]
    public async Task DeleteExpense_ByNonCreator_Returns403()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var topicResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var payerClient = factory.CreateClient();
        var payer = await AuthTestHelpers.SignInAsNewUserAsync(payerClient, factory.GoogleTokenValidator);
        await payerClient.PostAsync($"/api/topics/join/{topic!.InviteCode}", null);

        // The owner logs an expense on the payer's behalf - "payer" is not
        // the creator (FR-12a's explicitly called-out edge case: even the
        // payer can't delete it if they didn't log it).
        var createResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{topic.Id}/expenses", new
        {
            description = "Taxi",
            amount = 12.00m,
            paidByUserId = payer.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, payer.Id },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var deleteResponse = await payerClient.DeleteAsync($"/api/expenses/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task GetExpenses_ListsAcrossSubtopicTreeWithPayerAndParticipants()
    {
        var client = factory.CreateClient();
        var user = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var rootResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var root = await rootResponse.Content.ReadFromJsonAsync<TopicResponse>();
        var subtopicResponse = await client.PostAsJsonAsync($"/api/topics/{root!.Id}/subtopics", new { name = "Fuel" });
        var subtopic = await subtopicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var rootExpenseResponse = await client.PostAsJsonAsync($"/api/topics/{root.Id}/expenses", new
        {
            description = "Groceries",
            amount = 20.00m,
            paidByUserId = user.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { user.Id },
        });
        var rootExpense = await rootExpenseResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var subExpenseResponse = await client.PostAsJsonAsync($"/api/topics/{subtopic!.Id}/expenses", new
        {
            description = "Petrol",
            amount = 40.00m,
            paidByUserId = user.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { user.Id },
        });
        var subExpense = await subExpenseResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var listResponse = await client.GetAsync($"/api/topics/{root.Id}/expenses");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ExpenseResponse>>();

        Assert.Contains(list!, e => e.Id == rootExpense!.Id && e.PaidByUserId == user.Id);
        Assert.Contains(list!, e => e.Id == subExpense!.Id && e.PaidByUserId == user.Id);
    }

    [Fact]
    public async Task AddingMemberAfterExpenseCreated_DoesNotAlterExistingExpenseShares()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var topicResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var createResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{topic!.Id}/expenses", new
        {
            description = "Solo dinner",
            amount = 10.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        var laterMemberClient = factory.CreateClient();
        await AuthTestHelpers.SignInAsNewUserAsync(laterMemberClient, factory.GoogleTokenValidator);
        await laterMemberClient.PostAsync($"/api/topics/join/{topic.InviteCode}", null);

        var listResponse = await ownerClient.GetAsync($"/api/topics/{topic.Id}/expenses");
        var list = await listResponse.Content.ReadFromJsonAsync<List<ExpenseResponse>>();
        var stillThere = list!.Single(e => e.Id == created!.Id);

        Assert.Single(stillThere.Participants);
        Assert.Equal(owner.Id, stillThere.Participants[0].UserId);
        Assert.Equal(10.00m, stillThere.Participants[0].ShareAmount);
    }

    [Fact]
    public async Task PostExpense_ResponseAlwaysReportsAmountInEur()
    {
        var client = factory.CreateClient();
        var user = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var topicResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var response = await client.PostAsJsonAsync($"/api/topics/{topic!.Id}/expenses", new
        {
            description = "Coffee",
            amount = 3.50m,
            paidByUserId = user.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { user.Id },
        });

        // Structural proof (FR-18): the response has a plain numeric amount
        // and no currency field/selector at all - EUR is implicit and fixed,
        // not a value the API exposes as configurable.
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Number, root.GetProperty("amount").ValueKind);
        Assert.False(root.TryGetProperty("currency", out _));
    }

    [Fact]
    public async Task PostExpense_WithDuplicateParticipantIds_TreatsAsDistinctParticipants()
    {
        var client = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var topicResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{topic!.InviteCode}", null);

        // owner.Id listed twice - must not throw (ExpenseParticipant's PK is
        // the (ExpenseId, UserId) pair) and must not double-count owner's
        // share; treated exactly as if only [owner, member] were tagged.
        var response = await client.PostAsJsonAsync($"/api/topics/{topic.Id}/expenses", new
        {
            description = "Dinner",
            amount = 30.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, owner.Id, member.Id },
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expense = await response.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.Equal(2, expense!.Participants.Count);
        Assert.Equal(15.00m, expense.Participants.Single(p => p.UserId == owner.Id).ShareAmount);
        Assert.Equal(15.00m, expense.Participants.Single(p => p.UserId == member.Id).ShareAmount);
    }

    [Fact]
    public async Task PostExpense_WithNonMemberParticipant_Returns400()
    {
        var client = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var topicResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var outsiderClient = factory.CreateClient();
        var outsider = await AuthTestHelpers.SignInAsNewUserAsync(outsiderClient, factory.GoogleTokenValidator);
        // outsider never joins topic

        var response = await client.PostAsJsonAsync($"/api/topics/{topic!.Id}/expenses", new
        {
            description = "Dinner",
            amount = 20.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, outsider.Id },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostExpense_WithNonPositiveAmount_Returns400()
    {
        var client = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(client, factory.GoogleTokenValidator);
        var topicResponse = await client.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var response = await client.PostAsJsonAsync($"/api/topics/{topic!.Id}/expenses", new
        {
            description = "Free thing",
            amount = 0.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutExpense_RemovingAParticipant_LeavesOnlyTheRemainingParticipants()
    {
        var ownerClient = factory.CreateClient();
        var owner = await AuthTestHelpers.SignInAsNewUserAsync(ownerClient, factory.GoogleTokenValidator);
        var topicResponse = await ownerClient.PostAsJsonAsync("/api/topics", new { name = "Trip" });
        var topic = await topicResponse.Content.ReadFromJsonAsync<TopicResponse>();

        var memberClient = factory.CreateClient();
        var member = await AuthTestHelpers.SignInAsNewUserAsync(memberClient, factory.GoogleTokenValidator);
        await memberClient.PostAsync($"/api/topics/join/{topic!.InviteCode}", null);

        var createResponse = await ownerClient.PostAsJsonAsync($"/api/topics/{topic.Id}/expenses", new
        {
            description = "Dinner",
            amount = 20.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id, member.Id },
        });
        var created = await createResponse.Content.ReadFromJsonAsync<ExpenseResponse>();

        // Removes "member", leaving only "owner" - proves the old
        // participant set is fully replaced, not unioned with the new one.
        var editResponse = await ownerClient.PutAsJsonAsync($"/api/expenses/{created!.Id}", new
        {
            description = "Dinner",
            amount = 20.00m,
            paidByUserId = owner.Id,
            expenseDate = DateTimeOffset.UtcNow,
            participantUserIds = new[] { owner.Id },
        });

        Assert.Equal(HttpStatusCode.OK, editResponse.StatusCode);
        var edited = await editResponse.Content.ReadFromJsonAsync<ExpenseResponse>();
        Assert.Single(edited!.Participants);
        Assert.Equal(owner.Id, edited.Participants[0].UserId);
        Assert.Equal(20.00m, edited.Participants[0].ShareAmount);
    }
}
