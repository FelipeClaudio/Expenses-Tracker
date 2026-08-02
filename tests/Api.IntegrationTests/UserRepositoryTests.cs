using Core.Auth;
using Core.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

/// <summary>
/// Exercises IUserRepository directly against the real Postgres container
/// (bypassing HTTP) to test persistence-layer behavior that isn't otherwise
/// observable through the auth endpoints, notably the concurrent-first-sign-in
/// race: two requests for the same brand-new Google account can both decide
/// "no existing user" before either commits its insert.
/// </summary>
[Collection(ApiTestCollection.Name)]
public class UserRepositoryTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task AddAsync_ConcurrentInsertForSameGoogleSubjectId_ReturnsTheSamePersistedUserInsteadOfThrowing()
    {
        using var scope = factory.Services.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var subject = $"google-subject-{Guid.NewGuid()}";
        var email = "race@example.com";

        // Simulates two requests that both already checked
        // FindByGoogleSubjectIdAsync (got null) before either committed its
        // insert - the second AddAsync call is the one that loses the race
        // against the unique index on GoogleSubjectId.
        var firstCandidate = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = subject,
            Email = email,
            DisplayName = "Race Winner",
            CreatedAt = clock.UtcNow,
        };
        var secondCandidate = new User
        {
            Id = Guid.NewGuid(),
            GoogleSubjectId = subject,
            Email = email,
            DisplayName = "Race Loser",
            CreatedAt = clock.UtcNow,
        };

        var firstResult = await userRepository.AddAsync(firstCandidate);
        var secondResult = await userRepository.AddAsync(secondCandidate);

        Assert.Equal(firstResult.Id, secondResult.Id);
        Assert.Equal(firstCandidate.Id, secondResult.Id);
    }
}
