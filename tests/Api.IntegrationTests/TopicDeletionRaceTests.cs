using Core.Auth;
using Core.Expenses;
using Core.Topics;
using Core.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

/// <summary>
/// Exercises ITopicRepository.DeleteAsync directly against the real Postgres
/// container (bypassing HTTP), mirroring the concurrency coverage already
/// added for UserRepository/TopicMembershipRepository: two independent DI
/// scopes = two independent DbContext instances, exactly like two separate
/// concurrent HTTP requests.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class TopicDeletionRaceTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task DeleteAsync_ConcurrentDeleteOfSameTopic_DoesNotThrow()
    {
        var (rootId, _) = await CreateRootTopicAsync();

        // Two independent requests both already fetched this topic (and its
        // then-empty descendant list) before either commits its delete - the
        // second one's delete matches zero rows, since the first already
        // removed it. That must be treated as an already-achieved end state,
        // not an error.
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();
        var topicRepository1 = scope1.ServiceProvider.GetRequiredService<ITopicRepository>();
        var topicRepository2 = scope2.ServiceProvider.GetRequiredService<ITopicRepository>();

        var topicInScope1 = await topicRepository1.FindByIdAsync(rootId);
        var topicInScope2 = await topicRepository2.FindByIdAsync(rootId);

        await topicRepository1.DeleteAsync(topicInScope1!, []);

        var exception = await Record.ExceptionAsync(() => topicRepository2.DeleteAsync(topicInScope2!, []));

        Assert.Null(exception);
    }

    [Fact]
    public async Task DeleteAsync_TopicGainsChildConcurrently_ThrowsTopicDeletionConflictException()
    {
        var (rootId, _) = await CreateRootTopicAsync();

        using var staleScope = factory.Services.CreateScope();
        var staleTopicRepository = staleScope.ServiceProvider.GetRequiredService<ITopicRepository>();
        var staleTopic = await staleTopicRepository.FindByIdAsync(rootId);
        var staleDescendants = await staleTopicRepository.FindDescendantsAsync(staleTopic!);
        Assert.Empty(staleDescendants); // sanity check: nothing under it yet

        // A concurrent request adds a subtopic under the same root - this
        // commits (and is visible in the database) before the stale delete
        // below runs, but staleDescendants above was captured before it
        // existed.
        using var concurrentScope = factory.Services.CreateScope();
        var concurrentTopicRepository = concurrentScope.ServiceProvider.GetRequiredService<ITopicRepository>();
        var clock = concurrentScope.ServiceProvider.GetRequiredService<IClock>();
        await concurrentTopicRepository.AddAsync(new Topic
        {
            Id = Guid.NewGuid(),
            ParentTopicId = rootId,
            RootTopicId = rootId,
            Name = "Snuck In Concurrently",
            CreatedByUserId = staleTopic!.CreatedByUserId,
            CreatedAt = clock.UtcNow,
        });

        await Assert.ThrowsAsync<TopicDeletionConflictException>(
            () => staleTopicRepository.DeleteAsync(staleTopic, staleDescendants));
    }

    [Fact]
    public async Task DeleteAsync_TopicGainsExpenseConcurrently_StillDeletesCleanly()
    {
        // Unlike a concurrently-added child Topic (which the parallel
        // FK-violation test above proves throws TopicDeletionConflictException,
        // since it's found via the stale `descendants` snapshot the caller
        // passed in), a concurrently-added Expense on the topic itself is
        // NOT a race at all: DeleteAsync re-queries dbContext.Expenses by
        // topic id live, and topic.Id itself (unlike the descendants list)
        // is never stale. So this is the one case where "generalizes to
        // Expense automatically" actually means "there's no race to catch,"
        // not "the same exception fires" - confirmed here rather than
        // assumed.
        var (rootId, creatorUserId) = await CreateRootTopicAsync();

        using var staleScope = factory.Services.CreateScope();
        var staleTopicRepository = staleScope.ServiceProvider.GetRequiredService<ITopicRepository>();
        var staleTopic = await staleTopicRepository.FindByIdAsync(rootId);
        var staleDescendants = await staleTopicRepository.FindDescendantsAsync(staleTopic!);
        Assert.Empty(staleDescendants);

        using var concurrentScope = factory.Services.CreateScope();
        var concurrentExpenseRepository = concurrentScope.ServiceProvider.GetRequiredService<IExpenseRepository>();
        var clock = concurrentScope.ServiceProvider.GetRequiredService<IClock>();
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            TopicId = rootId,
            Description = "Snuck In Concurrently",
            Amount = 10.00m,
            PaidByUserId = creatorUserId,
            ExpenseDate = clock.UtcNow,
            CreatedByUserId = creatorUserId,
            CreatedAt = clock.UtcNow,
        };
        await concurrentExpenseRepository.AddAsync(expense, [new ExpenseParticipant
        {
            ExpenseId = expense.Id,
            UserId = creatorUserId,
            ShareAmount = 10.00m,
        }]);

        var exception = await Record.ExceptionAsync(() => staleTopicRepository.DeleteAsync(staleTopic!, staleDescendants));

        Assert.Null(exception);
    }

    private async Task<(Guid RootId, Guid CreatorUserId)> CreateRootTopicAsync()
    {
        using var scope = factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var topicRepository = scope.ServiceProvider.GetRequiredService<ITopicRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var user = await userRepository.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = $"google-subject-{Guid.NewGuid()}",
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = "Race Test User",
            CreatedAt = clock.UtcNow,
        });

        var rootId = Guid.NewGuid();
        var root = await topicRepository.AddAsync(new Topic
        {
            Id = rootId,
            ParentTopicId = null,
            RootTopicId = rootId,
            Name = "Race Topic",
            InviteCode = Guid.NewGuid().ToString("N"),
            CreatedByUserId = user.Id,
            CreatedAt = clock.UtcNow,
        });

        return (root.Id, user.Id);
    }
}
