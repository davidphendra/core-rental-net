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
    public void The_happy_path_offers_the_three_tiers_the_contract_defines()
    {
        var body = ScenarioLibrary.For("essential");

        Assert.Contains("\"status\":\"ok\"", body, StringComparison.Ordinal);

        foreach (var tier in new[] { "low", "middle", "high" })
        {
            Assert.Contains($"\"tier\":\"{tier}\"", body, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Every scenario stays inside the contract's closed vocabularies.
    /// </summary>
    /// <remarks>
    /// The first version of the fixture offered tiers named "essential" and "premium" and a status of
    /// "unavailable", and the schema allows none of them - and every test passed, because the
    /// application faithfully rendered what it was handed. A fixture outside the contract tests the
    /// application against a language it will never hear, so this is the test that keeps the stand-in
    /// speaking the agent's own.
    /// </remarks>
    [Fact]
    public void Every_scenario_stays_inside_the_contracts_closed_vocabularies()
    {
        var statuses = new[] { "ok", "exhausted", "rejected" };
        var tiers = new[] { "low", "middle", "high" };
        var findings = new[] { "criteria_not_met", "tier_composition" };
        var criteria = new System.Text.RegularExpressions.Regex("^(slot|quantity|tag|attribute):[a-z0-9:.-]+$");
        var slots = Enum.GetNames<CoreRentalNet.Modules.Workspace.Domain.SlotId>();

        foreach (var (name, body) in ScenarioLibrary.All)
        {
            foreach (var line in Lines(body))
            {
                using var document = JsonDocument.Parse(line);
                var root = document.RootElement;

                if (root.TryGetProperty("kind", out var kind) && kind.GetString() == "stage")
                {
                    continue;
                }

                Assert.Contains(root.GetProperty("status").GetString()!, statuses);

                if (root.GetProperty("status").GetString() == "rejected")
                {
                    Assert.True(root.TryGetProperty("code", out _), $"{name}: a refusal carries its code");
                    Assert.Empty(root.GetProperty("options").EnumerateArray());
                }

                foreach (var option in root.GetProperty("options").EnumerateArray())
                {
                    Assert.Contains(option.GetProperty("tier").GetString()!, tiers);

                    // The line's slot is a closed list too, and it is the one the apply depends on: a
                    // composition naming "desk" rather than "Desk" would be refused by the workspace at
                    // the last moment, having been accepted by everything before it.
                    foreach (var item in option.GetProperty("lines").EnumerateArray())
                    {
                        Assert.Contains(item.GetProperty("slot").GetString()!, slots);
                    }

                    foreach (var token in option.GetProperty("criteria").EnumerateArray())
                    {
                        Assert.Matches(criteria, token.GetString());
                    }

                    foreach (var unmet in option.GetProperty("unevaluated").EnumerateArray())
                    {
                        Assert.False(string.IsNullOrWhiteSpace(unmet.GetProperty("phrase").GetString()));
                    }
                }

                if (root.TryGetProperty("findings", out var objections))
                {
                    foreach (var finding in objections.EnumerateArray())
                    {
                        Assert.Contains(finding.GetProperty("kind").GetString()!, findings);
                        Assert.False(string.IsNullOrWhiteSpace(finding.GetProperty("slot").GetString()));
                    }
                }
            }
        }
    }

    /// <summary>The lines that are JSON, which is not all of them in the scenario that says so.</summary>
    private static IEnumerable<string> Lines(string body)
        => body.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.TrimStart().StartsWith('{'))
            .Select(line => line.Trim());

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
