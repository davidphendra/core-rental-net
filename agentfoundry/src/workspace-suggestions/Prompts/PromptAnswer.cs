using System.Text.Json;
using Microsoft.Extensions.AI;

namespace AgentFoundry.WorkspaceSuggestions.Prompts;

/// <summary>Asks a model, and takes the one JSON value out of what comes back.</summary>
/// <remarks>
/// <para>
/// The prompt asks for nothing before the answer and nothing after it, and models mostly oblige. The ones
/// that do not tend to wrap it in a sentence, so the value is found rather than assumed - which is
/// forgiving about packaging and strict about content, because a reply with no JSON in it at all is not
/// an answer and is refused rather than read as an empty one.
/// </para>
/// <para>
/// Both shapes are read. One prompt's answer is an object describing the request and another's is a list
/// of the parts it means, and a reader that only understood one of them would have failed on the other
/// with a message about missing braces.
/// </para>
/// </remarks>
internal static class PromptAnswer
{
    public static async Task<string> JsonAsync(
        IChatClient client,
        string prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client
            .GetResponseAsync([new ChatMessage(ChatRole.User, prompt)], cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return Json(response.Text ?? string.Empty);
    }

    /// <summary>
    /// The first JSON value in the text, or a refusal.
    /// </summary>
    /// <remarks>
    /// From the first brace or bracket to the last of either, then validated by parsing: a model that
    /// wrote a sentence, then an object, then another sentence is read correctly, and one that wrote no
    /// JSON at all is refused.
    /// </remarks>
    public static string Json(string text)
    {
        var start = Index(text, '{', '[');
        var end = Math.Max(text.LastIndexOf('}'), text.LastIndexOf(']'));

        if (start < 0 || end <= start)
        {
            throw new PromptAnswerException("The model's answer held no JSON to read.");
        }

        var candidate = text[start..(end + 1)];

        try
        {
            using var document = JsonDocument.Parse(candidate);

            return candidate;
        }
        catch (JsonException failure)
        {
            throw new PromptAnswerException($"The model's answer was not readable JSON: {failure.Message}");
        }
    }

    private static int Index(string text, char first, char second)
    {
        var one = text.IndexOf(first, StringComparison.Ordinal);
        var other = text.IndexOf(second, StringComparison.Ordinal);

        return one < 0 ? other : other < 0 ? one : Math.Min(one, other);
    }
}
