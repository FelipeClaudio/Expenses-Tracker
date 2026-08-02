using Api.IntegrationTests.Fakes;
using Core.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Api.IntegrationTests;

/// <summary>
/// Boots the real API pipeline against an ephemeral, real Postgres
/// container (NFR-6) with the Google token validator swapped for a
/// deterministic fake (so tests never call Google's servers).
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("expenses_tracker_test")
        .WithUsername("expenses_tracker_test")
        .WithPassword("expenses_tracker_test")
        .Build();

    public FakeGoogleTokenValidator GoogleTokenValidator { get; } = new();

    public FakeClock Clock { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Force the host to build now (rather than lazily on first request)
        // so migrations run before any test issues an HTTP call.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.StopAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator>(GoogleTokenValidator);

            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);
        });
    }
}
