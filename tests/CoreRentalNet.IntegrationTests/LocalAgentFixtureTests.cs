using System.Text.Json;
using CoreRentalNet.E2E.LocalAgent;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The stand-in agent's scenarios, held against the catalogue they are written about.
/// </summary>
/// <remarks>
/// The stand-in names SKUs, and a SKU the catalogue does not have would be dropped by the application in
/// every scenario - so every scenario would exercise the drop path, the happy path would be tested
/// nowhere, and nothing would say so. That is the kind of failure a fixture makes possible and this is
/// the test that makes it impossible.
/// </remarks>
public sealed class LocalAgentFixtureTests
{
    [Fact]
    public void Every_scenario_is_one_the_stand_in_offers()
    {
        Assert.NotEmpty(ScenarioLibrary.Names);

        Assert.All(ScenarioLibrary.Names, name => Assert.False(string.IsNullOrWhiteSpace(ScenarioLibrary.For(name))));
    }

    [Fact]
    public void Every_sku_a_scenario_names_is_one_the_catalogue_has()
    {
        var catalogue = Catalogue();

        var named = ScenarioLibrary.Skus;

        Assert.NotEmpty(named);

        var missing = named.Where(sku => !catalogue.Contains(sku)).Distinct().ToList();

        Assert.True(
            missing.Count == 0,
            $"the stand-in names SKUs the catalogue does not have: {string.Join(", ", missing)}");
    }

    [Fact]
    public void The_happy_path_offers_the_three_tiers_the_application_renders()
    {
        var body = ScenarioLibrary.For("essential");

        Assert.Contains("\"status\":\"ok\"", body, StringComparison.Ordinal);

        foreach (var tier in new[] { "essential", "balanced", "premium" })
        {
            Assert.Contains($"\"tier\":\"{tier}\"", body, StringComparison.Ordinal);
        }
    }

    private static HashSet<string> Catalogue()
    {
        var path = Path.Combine(RepositoryRoot(), "src", "shared", "data", "products.json");

        using var document = JsonDocument.Parse(File.ReadAllText(path));

        return
        [
            .. document.RootElement.EnumerateArray()
                .Select(product => product.GetProperty("skuNo").GetString()!),
        ];
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "CLAUDE.md")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("no repository root above the test output");
    }
}
