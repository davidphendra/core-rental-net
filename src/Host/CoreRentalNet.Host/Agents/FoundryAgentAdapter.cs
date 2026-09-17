using System.Runtime.CompilerServices;
using System.Text.Json;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

namespace CoreRentalNet.Host.Agents;

/// <summary>
/// Turns what the hosted agent streams into the messages the application's port speaks.
/// </summary>
/// <remarks>
/// <para>
/// The platform streams text, and our contract is JSON, so the one thing this class does is carry a
/// each streamed message across that boundary. The agent sends one JSON object per update - a stage or
/// the result - and a message it cannot read is skipped rather than guessed at: a stage we drop costs a
/// line of progress, and a result we guessed at would be a candidate nobody composed.
/// </para>
/// <para>
/// Updates are accumulated and split on their own line boundaries, because a streamed update is a chunk
/// and not a message: the platform may split one JSON object across two updates, and treating each
/// chunk as a message would fail on exactly the runs that produced the most output.
/// </para>
/// </remarks>
internal sealed class FoundryAgentAdapter(IAgentTextStream stream, string? agentName) : IAgentSuggestions
{
    /// <inheritdoc />
    public bool IsConfigured => !string.IsNullOrWhiteSpace(agentName);

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentSuggestionMessage> AskAsync(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var pending = string.Empty;

        await foreach (var chunk in stream.AskAsync(query, cancellationToken))
        {
            pending += chunk;

            while (Split(ref pending) is { } line)
            {
                if (Message(line) is { } message)
                {
                    yield return message;
                }
            }
        }
    }

    /// <summary>The next complete line, removed from the buffer, or null when none has arrived yet.</summary>
    private static string? Split(ref string pending)
    {
        var boundary = pending.IndexOf('\n', StringComparison.Ordinal);

        if (boundary < 0)
        {
            return null;
        }

        var line = pending[..boundary];
        pending = pending[(boundary + 1)..];

        return line.Trim();
    }

    /// <summary>One line of the stream, as a contract message, or null when it is not one.</summary>
    private static AgentSuggestionMessage? Message(string line)
    {
        if (line.Length == 0)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            return Kind(root) == "stage"
                ? AgentSuggestionMessage.StageEvent(Text(root, "stage") ?? string.Empty, Number(root, "attempt"))
                : AgentSuggestionMessage.Answer(Result(root));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AgentSuggestion Result(JsonElement root)
        => new(
            Text(root, "status") ?? SuggestionStatus.Unavailable,
            Text(root, "code"),
            Options(root),
            Texts(root, "findings"));

    private static IReadOnlyList<AgentSuggestionOption> Options(JsonElement root)
    {
        if (!root.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. options.EnumerateArray().Select(option => new AgentSuggestionOption(
                Text(option, "tier") ?? string.Empty,
                Lines(option),
                Texts(option, "criteria"),
                Texts(option, "unevaluated"),
                Texts(option, "pinnedSlots"))),
        ];
    }

    private static IReadOnlyList<AgentSuggestionLine> Lines(JsonElement option)
    {
        if (!option.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return
        [
            .. lines.EnumerateArray().Select(line => new AgentSuggestionLine(
                Text(line, "slot") ?? string.Empty,
                Text(line, "sku") ?? string.Empty,
                Number(line, "quantity"))),
        ];
    }

    /// <summary>
    /// The strings of an array, or of an array of objects' <c>phrase</c> or <c>kind</c>.
    /// </summary>
    /// <remarks>
    /// Unevaluated criteria and findings arrive as objects on the wire - a phrase with a reason, a kind
    /// with a slot - and the application renders only the part a customer reads. Reading the one field
    /// here keeps the wire shape out of the port.
    /// </remarks>
    private static IReadOnlyList<string> Texts(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var read = new List<string>();

        foreach (var value in values.EnumerateArray())
        {
            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Object => Text(value, "phrase") ?? Text(value, "kind"),
                _ => null,
            };

            if (!string.IsNullOrWhiteSpace(text))
            {
                read.Add(text);
            }
        }

        return read;
    }

    private static string? Kind(JsonElement root) => Text(root, "kind");

    private static string? Text(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int Number(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : 0;
}
