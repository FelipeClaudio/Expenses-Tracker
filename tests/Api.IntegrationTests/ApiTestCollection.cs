namespace Api.IntegrationTests;

/// <summary>
/// Shares one <see cref="CustomWebApplicationFactory"/> (and its Testcontainers
/// Postgres instance) across every integration test class, so each test run
/// pays the container-startup cost once rather than once per class.
/// </summary>
[CollectionDefinition(Name)]
public sealed class ApiTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Api";
}
