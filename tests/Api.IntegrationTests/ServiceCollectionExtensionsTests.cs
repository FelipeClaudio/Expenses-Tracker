using Core.Auth;
using Infrastructure;
using Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.IntegrationTests;

/// <summary>
/// Exercises the Auth:FakeMode DI branch in AddInfrastructure directly -
/// distinct from FakeGoogleTokenValidatorTests (which only unit-tests the
/// class in isolation) and from CustomWebApplicationFactory-based tests
/// (which always override IGoogleTokenValidator themselves, so they never
/// actually go through this branch). No Postgres needed: resolving
/// IGoogleTokenValidator never touches the DbContext.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private static IServiceProvider BuildProvider(bool fakeMode)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=unused;Username=unused;Password=unused",
                ["Auth:FakeMode"] = fakeMode.ToString(),
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddInfrastructure_WithFakeModeTrue_ResolvesFakeGoogleTokenValidator()
    {
        var provider = BuildProvider(fakeMode: true);

        var validator = provider.GetRequiredService<IGoogleTokenValidator>();

        Assert.IsType<FakeGoogleTokenValidator>(validator);
    }

    [Fact]
    public void AddInfrastructure_WithFakeModeFalse_ResolvesRealGoogleTokenValidator()
    {
        var provider = BuildProvider(fakeMode: false);

        var validator = provider.GetRequiredService<IGoogleTokenValidator>();

        Assert.IsType<GoogleTokenValidator>(validator);
    }
}
