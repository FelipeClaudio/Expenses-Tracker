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
        var csproj = LoadCoreCsproj();
        var referenceIncludes = csproj.Descendants("ProjectReference")
            .Concat(csproj.Descendants("PackageReference"))
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

    /// <summary>
    /// <c>PackageReference</c>/<c>ProjectReference</c> aren't the only way a
    /// project pulls in ASP.NET Core — <c>FrameworkReference</c> (what
    /// ASP.NET Core projects normally use for the shared framework) has no
    /// package name to pattern-match, so it needs its own check.
    /// </summary>
    [Fact]
    public void CoreProject_HasNoFrameworkReference()
    {
        var frameworkReferences = LoadCoreCsproj().Descendants("FrameworkReference").ToList();

        Assert.Empty(frameworkReferences);
    }

    /// <summary>
    /// Switching the project's Sdk attribute to Microsoft.NET.Sdk.Web (or
    /// .Worker, etc.) implicitly grants ASP.NET Core APIs with zero
    /// PackageReference/ProjectReference/FrameworkReference entries — the
    /// other checks in this class would stay green even though Core stopped
    /// being a plain class library.
    /// </summary>
    [Fact]
    public void CoreProject_UsesPlainClassLibrarySdk()
    {
        var sdk = LoadCoreCsproj().Root?.Attribute("Sdk")?.Value;

        Assert.Equal("Microsoft.NET.Sdk", sdk);
    }

    private static XDocument LoadCoreCsproj()
    {
        var csprojPath = Path.Combine(FindRepoRoot(), "src", "Core", "Core.csproj");
        Assert.True(File.Exists(csprojPath), $"Expected to find {csprojPath}");
        return XDocument.Load(csprojPath);
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
