using System.Text.Json;
using AwesomeAssertions;
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

    [Fact] // AUTH-08
    public void The_one_file_allowed_to_hold_a_local_secret_is_gitignored()
    {
        var ignore = File.ReadAllText(Path.Combine(RepoRoot.Path, ".gitignore"));

        ignore.Should().Contain("appsettings.Local.json",
            "otherwise the exclusion below would quietly become a hole");
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
