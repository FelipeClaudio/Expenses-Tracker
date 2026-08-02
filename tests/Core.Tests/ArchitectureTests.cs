using System.Xml.Linq;

namespace Core.Tests;

public class ArchitectureTests
{
    private static readonly string[] ForbiddenReferenceSubstrings =
    [
        "Infrastructure",
        "AspNetCore",
        "EntityFrameworkCore",
        "Npgsql",
    ];

    /// <summary>
    /// Core must stay a pure, DB-free, framework-free class library so
    /// SettlementService/ExpenseSplitService can be unit-tested in isolation
    /// (spec §7). This inspects Core.csproj directly rather than adding a
    /// NetArchTest-style dependency for a single structural check.
    /// </summary>
    [Fact]
    public void CoreProject_HasNoInfrastructureOrAspNetReferences()
    {
        var csprojPath = Path.Combine(FindRepoRoot(), "src", "Core", "Core.csproj");
        Assert.True(File.Exists(csprojPath), $"Expected to find {csprojPath}");

        var doc = XDocument.Load(csprojPath);
        var referenceIncludes = doc.Descendants("ProjectReference")
            .Concat(doc.Descendants("PackageReference"))
            .Select(e => e.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        foreach (var include in referenceIncludes)
        {
            foreach (var forbidden in ForbiddenReferenceSubstrings)
            {
                Assert.False(
                    include.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"Core.csproj references '{include}', which pulls in '{forbidden}' — Core must stay infrastructure-free.");
            }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && dir.GetFiles("Expenses-Tracker.slnx").Length == 0)
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException(
                "Could not locate repo root (Expenses-Tracker.slnx) from test binary directory.");
    }
}
