using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using AwesomeAssertions;
using System.Text.RegularExpressions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The prompts, held to the format the loader will depend on and to the vocabularies the code owns.
/// </summary>
/// <remarks>
/// A prompt is sent to a model unchanged, so a mistake in one is not a compile error - it is a sentence
/// the model reads and does something with. These tests are the only thing between a typo and a model,
/// and they are deliberately about the prompt's <em>shape</em> and its use of closed vocabularies rather
/// than about its wording, which no test should try to own.
/// </remarks>
public sealed class PromptTests
{
    /// <summary>
    /// What the loader can fill. The list lives here until the loader exists, and moves beside it then.
    /// </summary>
    private static readonly IReadOnlySet<string> Placeholders = new HashSet<string>(StringComparer.Ordinal)
    {
        "slots",
        "query",
        "findings",
        "openMarker",
        "closeMarker",
    };

    public static TheoryData<string> PromptFiles() => [.. Directory
        .GetFiles(PromptsRoot("prompts"), "*.txt", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFileName)
        .Where(name => name is not null)
        .Select(name => name!)];

    /// <summary>Every prompt is a file that is only the prompt.</summary>
    /// <remarks>
    /// No markup, and nothing a writer would read as structure but a model would read as instruction.
    /// The file is sent verbatim, so a heading is not a heading - it is a line of the prompt.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PromptFiles))]
    public void A_prompt_file_is_only_the_prompt(string name)
    {
        var text = File.ReadAllText(Path.Combine(PromptsRoot("prompts"), name));

        text.Should().NotBeNullOrWhiteSpace("a prompt that ships empty ships an empty instruction");
        text.Should().NotStartWith("#", "markdown a writer reads is just text to a model");
        text.Should().NotContain("<!--", "a comment is sent to the model like anything else");
    }

    /// <summary>Every placeholder is one the loader knows how to fill.</summary>
    /// <remarks>
    /// An unfilled placeholder does not fail - it arrives at the model as the literal characters
    /// <c>{{slots}}</c>, which reads as an instruction about a thing that is not there. Nothing else in
    /// the system would notice.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PromptFiles))]
    public void Every_placeholder_is_one_the_loader_can_fill(string name)
    {
        var text = File.ReadAllText(Path.Combine(PromptsRoot("prompts"), name));

        var used = Regex.Matches(text, @"\{\{(\w+)\}\}").Select(match => match.Groups[1].Value).Distinct();

        used.Should().NotBeEmpty("a prompt with no placeholders is a prompt about nothing");
        used.Should().BeSubsetOf(Placeholders);
    }

    /// <summary>
    /// The customer's words are delimited wherever they reach a prompt.
    /// </summary>
    /// <remarks>
    /// This is the claim the content-safety policy makes - that delimiters wrap content wherever it meets
    /// a prompt - and until now it described code that did not exist. A prompt that interpolates the
    /// query without wrapping it is the claim being false.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PromptFiles))]
    public void The_customers_words_are_delimited_wherever_they_appear(string name)
    {
        var text = File.ReadAllText(Path.Combine(PromptsRoot("prompts"), name));

        if (!text.Contains("{{query}}", StringComparison.Ordinal))
        {
            return;
        }

        var open = text.IndexOf("{{openMarker}}", StringComparison.Ordinal);
        var query = text.IndexOf("{{query}}", StringComparison.Ordinal);
        var close = text.IndexOf("{{closeMarker}}", StringComparison.Ordinal);

        open.Should().BeGreaterThanOrEqualTo(0, $"{name} takes a customer's words, so it must delimit them");
        open.Should().BeLessThan(query);
        query.Should().BeLessThan(close);
    }

    /// <summary>
    /// The intent prompt names the codes the contract allows, and the prompt and the enum cannot drift.
    /// </summary>
    /// <remarks>
    /// The verdict's code is written straight into the result, and the result schema's <c>code</c> enum
    /// holds exactly one value. A prompt that taught the model a second code would be teaching it to
    /// produce a result the contract forbids, so the prompt is held to the vocabulary in code.
    /// </remarks>
    [Fact]
    public void The_intent_prompt_names_the_only_codes_the_contract_allows()
    {
        var text = File.ReadAllText(Path.Combine(PromptsRoot("prompts"), "workspace-intent.txt"));

        text.Should().Contain(ReasonCodes.NotWorkspaceRequest);
        text.Should().Contain(ReasonCodes.Ok);
        text.Should().Contain("isWorkspaceRequest");
    }

    /// <summary>
    /// A part named in an example is a part that exists.
    /// </summary>
    /// <remarks>
    /// The examples are the cheapest way to teach the answer's shape, and they name real parts to do it.
    /// A renamed part would leave them teaching a name the code drops, and the prompt would look correct
    /// while being wrong.
    /// </remarks>
    [Theory]
    [MemberData(nameof(PromptFiles))]
    public void A_part_named_in_an_example_exists(string name)
    {
        var text = File.ReadAllText(Path.Combine(PromptsRoot("prompts"), name));

        foreach (Match array in Regex.Matches(text, @"\[(?:\x22\w+\x22(?:,\x22\w+\x22)*)\]"))
        {
            foreach (Match part in Regex.Matches(array.Value, @"\x22(\w+)\x22"))
            {
                Slots.All.Should().Contain(part.Groups[1].Value);
            }
        }
    }

    private static string PromptsRoot(string name)
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "AgentFoundry.sln")))
        {
            folder = folder.Parent;
        }

        folder.Should().NotBeNull("the prompts live in the agent's tree, above the test output");

        return Path.Combine(folder!.FullName, "shared", name);
    }
}
