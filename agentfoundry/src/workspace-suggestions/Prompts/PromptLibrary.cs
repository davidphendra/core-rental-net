using System.Text.RegularExpressions;

namespace AgentFoundry.WorkspaceSuggestions.Prompts;

/// <summary>
/// The prompts on disk, and the one thing that fills them.
/// </summary>
/// <remarks>
/// <para>
/// A prompt is a file whose whole contents are the prompt, so the only work here is substitution - and
/// the work is deliberately unforgiving. A placeholder nobody filled would not fail; it would arrive at
/// a model as the literal characters <c>{{slots}}</c> and read as an instruction about a part that is
/// not there. Nothing downstream would notice, so this refuses to render one.
/// </para>
/// <para>
/// The markers around untrusted text are made here, one pair per rendering. They carry a nonce, which is
/// what makes them a delimiter rather than decoration: the customer's words are the input, so a marker
/// that could be typed would be a marker that could be closed from the outside. The same pair wraps
/// every untrusted block in one rendering, because a nonce that cannot be guessed does not become
/// guessable by being used twice.
/// </para>
/// </remarks>
internal sealed partial class PromptLibrary(string root)
{
    /// <summary>The prompts beside the container's own files.</summary>
    public static PromptLibrary Beside(string directory)
        => new(Path.Combine(directory, "shared", "prompts"));

    public string Render(string name, IReadOnlyDictionary<string, string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);

        var path = Path.Combine(root, name + ".txt");

        if (!File.Exists(path))
        {
            throw new PromptAnswerException($"There is no '{name}' prompt at '{path}'.");
        }

        var nonce = Guid.NewGuid().ToString("N")[..6];

        var filled = new Dictionary<string, string>(values, StringComparer.Ordinal)
        {
            ["openMarker"] = $"<<data-{nonce}>>",
            ["closeMarker"] = $"<<end-{nonce}>>",
        };

        return Fill(File.ReadAllText(path), filled, name);
    }

    private static string Fill(string text, IReadOnlyDictionary<string, string> values, string name)
    {
        foreach (var placeholder in Placeholders(text))
        {
            if (!values.TryGetValue(placeholder, out var value))
            {
                throw new PromptAnswerException(
                    $"The '{name}' prompt asks for '{placeholder}' and nothing supplies it.");
            }

            text = text.Replace("{{" + placeholder + "}}", value, StringComparison.Ordinal);
        }

        return text;
    }

    private static IEnumerable<string> Placeholders(string text)
        => Placeholder().Matches(text).Select(match => match.Groups[1].Value).Distinct(StringComparer.Ordinal);

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    private static partial Regex Placeholder();
}
