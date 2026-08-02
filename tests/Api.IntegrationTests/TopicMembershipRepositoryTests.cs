using Core.Auth;
using Core.Topics;
using Core.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

/// <summary>
/// Exercises ITopicMembershipRepository directly against the real Postgres
/// container (bypassing HTTP), mirroring UserRepositoryTests' concurrent-
/// insert coverage: two requests can both pass IsMemberAsync (false) before
/// either commits its AddMemberAsync insert, racing against TopicMember's
/// composite (RootTopicId, UserId) primary key.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class TopicMembershipRepositoryTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task AddMemberAsync_ConcurrentJoinForSameRootTopicAndUser_DoesNotThrow()
    {
        using var setupScope = factory.Services.CreateScope();
        var userRepository = setupScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var topicRepository = setupScope.ServiceProvider.GetRequiredService<ITopicRepository>();
        var clock = setupScope.ServiceProvider.GetRequiredService<IClock>();

        var user = await userRepository.AddAsync(new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = $"google-subject-{Guid.NewGuid()}",
            Email = $"{Guid.NewGuid()}@example.com",
            DisplayName = "Race User",
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

        // Two separate DI scopes = two separate DbContext instances, exactly
        // like two independent concurrent HTTP requests each getting their
        // own scoped DbContext - simulates both having already checked
        // IsMemberAsync (false) before either commits its insert, racing
        // against the composite PK (RootTopicId, UserId). A single shared
        // scope wouldn't reproduce this: EF's own change tracker would reject
        // the second Add() client-side before ever reaching the database.
        using var requestScope1 = factory.Services.CreateScope();
        using var requestScope2 = factory.Services.CreateScope();
        var membershipRepository1 = requestScope1.ServiceProvider.GetRequiredService<ITopicMembershipRepository>();
        var membershipRepository2 = requestScope2.ServiceProvider.GetRequiredService<ITopicMembershipRepository>();

        await membershipRepository1.AddMemberAsync(new TopicMember
        {
            RootTopicId = root.Id,
            UserId = user.Id,
            JoinedAt = clock.UtcNow,
        });

        await membershipRepository2.AddMemberAsync(new TopicMember
        {
            RootTopicId = root.Id,
            UserId = user.Id,
            JoinedAt = clock.UtcNow,
        });

        var membershipRepository = setupScope.ServiceProvider.GetRequiredService<ITopicMembershipRepository>();
        Assert.True(await membershipRepository.IsMemberAsync(root.Id, user.Id));
    }
}
