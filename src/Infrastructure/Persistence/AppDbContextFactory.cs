using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` construct an <see cref="AppDbContext"/>
/// at design time without needing the full Api host or a real connection
/// string — this is never used at runtime, where
/// <see cref="ServiceCollectionExtensions.AddInfrastructure"/> wires the
/// actual connection string from configuration.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=expenses_tracker_designtime;Username=postgres;Password=postgres");
        return new AppDbContext(optionsBuilder.Options);
    }
}
