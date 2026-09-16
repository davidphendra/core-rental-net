using System.Globalization;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// The client secret belongs in user secrets locally and in the environment otherwise, never in a
/// file that is committed. "We will be careful" is not a control, so this is one.
/// </summary>
public sealed class CommittedConfigurationTests
{
    [Fact] // AUTH-08
    public void No_identity_secret_is_committed_to_configuration()
    {
        var offenders = new List<string>();

        foreach (var file in ConfigurationFiles())
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));

            foreach (var secret in FindKeys(document.RootElement, "ClientSecret"))
            {
                if (!string.IsNullOrWhiteSpace(secret))
                {
                    offenders.Add($"{Path.GetRelativePath(RepoRoot.Path, file)} sets ClientSecret");
                }
            }
        }

        offenders.Should().BeEmpty(
            "the client secret comes from user secrets locally and from the environment otherwise");
    }

    [Fact] // MON-06
    public void The_configured_culture_is_the_one_whose_money_this_application_settles_in()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "appsettings.json")));

        var name = document.RootElement.GetProperty("Culture").GetProperty("Name").GetString();

        name.Should().NotBeNullOrWhiteSpace(
            "a server runs under the invariant culture, under which an amount reads \u00a4400,000.00");

        var culture = new CultureInfo(name, useUserOverride: false);
        var region = new RegionInfo(name);

        // Which money a region uses is read from the locale rather than written here: en-ID names the
        // rupiah and draws it "Rp", en-US names the dollar and draws it "$". This is the assertion
        // that ties the two together - that the locale this application is configured with is the
        // one whose money it charges. Configure en-US and it is the only thing that fails: every page
        // still renders, every money unit test still passes, and every amount is simply dollars.
        // The currency an amount is stated in is the amount's own (Money.Currency, read from the
        // catalog); the culture only decides how that amount is written. The two are kept in step on
        // purpose: the catalog must state the settlement currency (a product priced in another one is
        // refused at load), and en-ID is the culture that writes that currency the way Denpasar reads
        // it. This test is the assertion that the two halves describe the same money.
        region.ISOCurrencySymbol.Should().Be(
            Currencies.Idr,
            $"the configured culture is '{name}', and its region uses {region.ISOCurrencySymbol}");

        // And what an amount draws is that locale's symbol, because the symbol is the culture's and
        // not a prefix in the markup. en-ID draws "Rp0" the way id-ID did.
        0m.ToString("C", culture).Should().Be($"{region.CurrencySymbol}0");
    }

    [Fact] // AUTH-08
    public void User_secrets_are_enabled_so_there_is_somewhere_else_to_put_it()
    {
        var project = File.ReadAllText(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "CoreRentalNet.Host.csproj"));

        project.Should().Contain(
            "<UserSecretsId>",
            "without it there is no supported place for a development secret, which is how secrets end up in files");
    }

    [Fact] // AUTH-01
    public void Identity_configuration_stays_optional_and_unset_by_default()
    {
        var development = File.ReadAllText(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "appsettings.Development.json"));

        using var document = JsonDocument.Parse(development);

        document.RootElement.TryGetProperty("Auth0", out var auth0).Should().BeTrue(
            "the development file is where the developer pastes their tenant details");

        auth0.GetProperty("Domain").GetString().Should().BeNullOrEmpty("the repository ships unconfigured");
        auth0.GetProperty("ClientId").GetString().Should().BeNullOrEmpty("the repository ships unconfigured");
        auth0.TryGetProperty("ClientSecret", out _).Should().BeFalse("there is nowhere in a committed file for it");
    }

    [Fact] // AUTH-01
    public void The_local_settings_file_is_loaded_in_development_and_nowhere_else()
    {
        // Whichever file loads it, rather than a file named in advance: the guard moved to
        // Composition/LocalSettings.cs when the composition root was split, and a test pinned to a
        // path would have to be edited every time the code moved.
        var source = Directory
            .GetFiles(RepoRoot.Combine("src", "Host", "CoreRentalNet.Host"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal) && !file.Contains("/bin/", StringComparison.Ordinal))
            .Single(file => File.ReadAllText(file).Contains("appsettings.Local.json", StringComparison.Ordinal));

        var program = File.ReadAllText(source);

        var guard = program.IndexOf("if (builder.Environment.IsDevelopment())", StringComparison.Ordinal);
        var load = program.IndexOf("appsettings.Local.json", StringComparison.Ordinal);

        guard.Should().BeGreaterThanOrEqualTo(0, "the local file has to be guarded by an environment check");
        load.Should().BeGreaterThan(guard, "the file must be loaded inside that guard, not before it");

        // And inside it rather than after it: no closing brace may appear in between.
        var between = program[guard..load];
        between.Should().Contain("{").And.NotContain(
            "}", "otherwise the guard has already ended and the file is loaded unconditionally");
    }

    [Fact] // AUTH-08
    public void The_one_file_allowed_to_hold_a_local_secret_is_gitignored()
    {
        var ignore = File.ReadAllText(Path.Combine(RepoRoot.Path, ".gitignore"));

        ignore.Should().Contain("appsettings.Local.json",
            "otherwise the exclusion below would quietly become a hole");
    }

    [Fact] // SLOT-05
    public void The_committed_slot_capacities_are_the_shipped_defaults()
    {
        // The table moves to configuration, so a committed number and the code's default can drift
        // apart: both still work, and what an operator reads stops being what the application ships.
        using var document = JsonDocument.Parse(File.ReadAllText(
            RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "appsettings.json")));

        var configured = document.RootElement.GetProperty("Workspace").GetProperty("SlotCapacity");
        var defaults = new WorkspaceSlotSettings();

        foreach (var slot in Enum.GetValues<SlotId>())
        {
            configured.GetProperty(slot.ToString()).GetInt32().Should().Be(
                defaults.CapacityFor(slot),
                $"the committed {slot} capacity and the shipped default have drifted apart");
        }
    }

    private static IEnumerable<string> ConfigurationFiles()
        => Directory.GetFiles(RepoRoot.Combine("src"), "appsettings*.json", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal))
            // The one sanctioned exception, and the test above proves it is not committed.
            .Where(file => !file.EndsWith("appsettings.Local.json", StringComparison.Ordinal));

    private static IEnumerable<string?> FindKeys(JsonElement element, string key)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, key, StringComparison.Ordinal) && property.Value.ValueKind is JsonValueKind.String)
                    {
                        yield return property.Value.GetString();
                    }

                    foreach (var nested in FindKeys(property.Value, key))
                    {
                        yield return nested;
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nested in FindKeys(item, key))
                    {
                        yield return nested;
                    }
                }

                break;
        }
    }
}
