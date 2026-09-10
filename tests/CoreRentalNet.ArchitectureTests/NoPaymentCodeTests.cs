using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// ADR-0010 says this application takes no money. That is a claim about the code, so it is checked
/// against the code rather than asserted in a document.
/// </summary>
public sealed class NoPaymentCodeTests
{
    private static readonly string[] PaymentVocabulary =
    [
        "Stripe",
        "IPaymentGateway",
        "PaymentIntent",
        "webhook",
        "Webhook",
        "cardNumber",
        "PCI",
    ];

    [Fact] // SEC-04
    public void Nothing_in_the_solution_integrates_a_payment_provider()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);

            foreach (var word in PaymentVocabulary)
            {
                if (text.Contains(word, StringComparison.Ordinal))
                {
                    offenders.Add($"{Path.GetRelativePath(RepoRoot.Path, file)} mentions '{word}'");
                }
            }
        }

        offenders.Should().BeEmpty(
            "there is no payment provider, no card handling and no webhook in this application (ADR-0010)");
    }

    [Fact] // SEC-04
    public void No_payment_package_is_referenced()
    {
        var offenders = ProjectFiles()
            .SelectMany(file => File.ReadAllLines(file).Select(line => (File: file, Line: line)))
            .Where(entry => entry.Line.Contains("Stripe", StringComparison.OrdinalIgnoreCase)
                            || entry.Line.Contains("Braintree", StringComparison.OrdinalIgnoreCase)
                            || entry.Line.Contains("PayPal", StringComparison.OrdinalIgnoreCase))
            .Select(entry => $"{Path.GetRelativePath(RepoRoot.Path, entry.File)}: {entry.Line.Trim()}")
            .ToArray();

        offenders.Should().BeEmpty();
    }

    private static IEnumerable<string> SourceFiles()
        => Directory
            .GetFiles(RepoRoot.Combine("src"), "*.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(RepoRoot.Combine("src"), "*.razor", SearchOption.AllDirectories))
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal))
            .Where(file => !file.Contains("/Migrations/", StringComparison.Ordinal));

    private static IEnumerable<string> ProjectFiles()
        => Directory.GetFiles(RepoRoot.Path, "*.csproj", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal))
            .Concat([Path.Combine(RepoRoot.Path, "Directory.Packages.props")]);
}
