using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>No published contract may declare a member that is a secret.</summary>
/// <remarks>
/// <para>
/// <b>A contract is a message, and a message is not a place for a credential.</b> What is declared on one is
/// recorded by the hosting layer, hashed into a run record, offered to a model as text, and persisted verbatim for
/// recovery — four places a secret must not be, from one declaration.
/// </para>
/// <para>
/// <b>Checked over the JSON schemas rather than over C#</b>, because the schemas are the artefact the two
/// deployables actually agree on, and a field removed from a record but left in a schema is exactly the kind of
/// drift this is meant to catch.
/// </para>
/// <para>
/// <b>The exception below is self-cleaning.</b> The assertion compares the offenders to a named list, so when the
/// last credential is taken off the wire the list no longer matches and this test fails until the exception is
/// deleted with it. An exception that cannot outlive its reason is the only kind worth having.
/// </para>
/// </remarks>
public sealed class NoWireContractCarriesASecretTests
{
    /// <summary>The nouns a credential's name ends with, which is what makes a declaration worth a look.</summary>
    private static readonly string[] NounsACredentialIsNamedWith = ["token", "secret", "password", "credential"];

    /// <summary>Nothing, now: the last credential left the wire when the carrier moved to the invocation.</summary>
    /// <remarks>
    /// <b>This list is the point of the test.</b> It was the exception that named the one offending member, and the
    /// assertion compares the offenders to it exactly — so emptying it here was not optional: while the member and
    /// the entry both existed the test would have kept passing, and leaving the entry behind once the member was
    /// gone would have failed. The rule outlived its reason, which is what an exception should do.
    /// </remarks>
    private static readonly string[] KnownOffenders = [];

    [Fact]
    public void No_published_contract_declares_a_member_that_is_a_secret()
    {
        var offenders = DeclaredMemberNamesInEveryContract()
            .Where(IsNamedLikeACredential)
            .Order(StringComparer.Ordinal);

        offenders.Should().Equal(
            KnownOffenders,
            "the list is the exception rather than the rule, so removing the field without removing its entry fails here");
    }

    [Fact] // the contract the exception used to name is still published, so the rule it enforced still applies
    public void The_contract_this_rule_was_written_for_is_still_published()
    {
        File.Exists(RepoRoot.Combine("agentfoundry", "shared", "contracts", "suggestion.request.schema.json"))
            .Should().BeTrue("the rule outlived the exception, so the contract it guards must still be there to check");
    }

    /// <summary>Whether a declared name says a credential rather than counting one.</summary>
    /// <remarks>
    /// <b>The last word is the noun, and that is the whole rule.</b> The run's usage is published as
    /// <c>inputTokens</c> and <c>outputTokens</c> — counts, and not credentials — so a rule that matched the word
    /// anywhere would be a rule nobody could keep. A name whose final word is <c>Token</c>, <c>Secret</c>,
    /// <c>Password</c> or <c>Credential</c> is claiming to carry one.
    /// </remarks>
    private static bool IsNamedLikeACredential(string declaredName)
    {
        var wordsInTheName = WordsIn(declaredName);

        return wordsInTheName.Length > 0
            && NounsACredentialIsNamedWith.Contains(wordsInTheName[^1], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>The words a camel-cased, underscored or hyphenated member name is made of.</summary>
    private static string[] WordsIn(string declaredName)
        => DeclaredNameWordBoundary.Split(declaredName).Where(word => word.Length > 0).ToArray();

    /// <summary>Where one word ends and the next begins: before a capital, and at a separator.</summary>
    private static readonly Regex DeclaredNameWordBoundary = new(@"(?<!^)(?=[A-Z])|[_-]", RegexOptions.Compiled);

    /// <summary>Every member name declared by every published contract, qualified by its file.</summary>
    private static IEnumerable<string> DeclaredMemberNamesInEveryContract()
    {
        var contractsDirectory = RepoRoot.Combine("agentfoundry", "shared", "contracts");

        foreach (var contractFile in Directory.EnumerateFiles(contractsDirectory, "*.schema.json"))
        {
            using var contract = JsonDocument.Parse(File.ReadAllText(contractFile));
            var declaredNames = new List<string>();

            CollectDeclaredMemberNames(contract.RootElement, declaredNames);

            foreach (var declaredName in declaredNames)
            {
                yield return $"{Path.GetFileName(contractFile)}: {declaredName}";
            }
        }
    }

    /// <summary>The names declared under any <c>properties</c> object, however deeply nested.</summary>
    private static void CollectDeclaredMemberNames(JsonElement element, List<string> declaredNames)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var member in element.EnumerateObject())
            {
                if (member.NameEquals("properties") && member.Value.ValueKind == JsonValueKind.Object)
                {
                    declaredNames.AddRange(member.Value.EnumerateObject().Select(declared => declared.Name));
                }

                CollectDeclaredMemberNames(member.Value, declaredNames);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectDeclaredMemberNames(item, declaredNames);
            }
        }
    }
}
