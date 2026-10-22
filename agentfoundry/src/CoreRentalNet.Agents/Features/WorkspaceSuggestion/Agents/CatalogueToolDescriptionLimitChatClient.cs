using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

/// <summary>Limits the description in the model-facing copy of a catalogue result.</summary>
/// <remarks>
/// <para>
/// <b>The full result has already been recorded before this decorator runs.</b> The ledger, the candidate pool,
/// the reranker and the evaluation trace keep the Host's full description; only the retriever model's copy is
/// shortened. That is what keeps the context smaller without removing the evidence later stages need.
/// </para>
/// <para>
/// <b>It parses the original JSON rather than the agent's mirror.</b> A mirror reserializes only the fields it
/// knows, which would silently drop properties such as the image path and the featured flag.
/// </para>
/// </remarks>
internal sealed class CatalogueToolDescriptionLimitChatClient(
    IChatClient innerClient,
    ILogger<CatalogueToolDescriptionLimitChatClient> logger)
    : DelegatingChatClient(innerClient)
{
    private const int MaximumDescriptionTextElementCount = 100;

    private readonly ILogger<CatalogueToolDescriptionLimitChatClient> _logger = logger;

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(LimitDescriptions(messages), options, cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(LimitDescriptions(messages), options, cancellationToken);

    private IReadOnlyList<ChatMessage> LimitDescriptions(IEnumerable<ChatMessage> messages)
    {
        var source = messages.ToArray();
        var resultMessageCount = source.Count(message => message.Contents.OfType<FunctionResultContent>().Any());
        if (resultMessageCount > 0)
        {
            _logger.LogInformation(
                "Catalogue description limit: {Count} tool-result message(s) reached the model-facing client.",
                resultMessageCount);
        }

        var toolNamesByCallId = source
            .SelectMany(message => message.Contents)
            .OfType<FunctionCallContent>()
            .Where(call => !string.IsNullOrEmpty(call.CallId))
            .ToDictionary(
                call => call.CallId!,
                call => call.Name,
                StringComparer.Ordinal);

        if (toolNamesByCallId.Count == 0)
        {
            return source;
        }

        var limited = new ChatMessage[source.Length];
        var changedAny = false;

        for (var index = 0; index < source.Length; index++)
        {
            limited[index] = LimitMessage(
                source[index],
                toolNamesByCallId,
                out var changed);

            changedAny |= changed;
        }

        if (changedAny)
        {
            _logger.LogInformation("Catalogue description limit: model-facing descriptions were shortened.");
        }

        return changedAny ? limited : source;
    }

    private static ChatMessage LimitMessage(
        ChatMessage message,
        IReadOnlyDictionary<string, string> toolNamesByCallId,
        out bool changed)
    {
        changed = false;

        if (!message.Contents.OfType<FunctionResultContent>().Any())
        {
            return message;
        }

        var contents = new List<AIContent>(message.Contents.Count);

        foreach (var content in message.Contents)
        {
            if (content is FunctionResultContent result
                && result.CallId is { } callId
                && TryResultText(result.Result, out var resultText)
                && IsCatalogueResult(callId, toolNamesByCallId)
                && TryLimitDescription(resultText, out var limitedText))
            {
                contents.Add(new FunctionResultContent(callId, limitedText));
                changed = true;
                continue;
            }

            contents.Add(content);
        }

        return changed
            ? new ChatMessage(message.Role, contents)
            : message;
    }

    private static bool IsCatalogueResult(
        string callId,
        IReadOnlyDictionary<string, string> toolNamesByCallId)
        => !toolNamesByCallId.TryGetValue(callId, out var toolName)
            || CatalogueSearchToolNames.All.Contains(toolName, StringComparer.Ordinal);

    private static bool TryResultText(object? result, out string resultText)
    {
        switch (result)
        {
            case string text:
                resultText = text;
                return true;
            case JsonElement element:
                resultText = element.GetRawText();
                return true;
            case JsonDocument document:
                resultText = document.RootElement.GetRawText();
                return true;
            case JsonNode node:
                resultText = node.ToJsonString();
                return true;
            default:
                resultText = result?.ToString() ?? string.Empty;
                return resultText.Length > 0;
        }
    }

    private static bool TryLimitDescription(string resultText, out string limitedText)
    {
        limitedText = resultText;

        try
        {
            if (JsonNode.Parse(resultText) is not JsonObject root
                || root["matches"] is not JsonObject matches
                || matches["value"] is not JsonArray products)
            {
                return false;
            }

            var changed = false;

            foreach (var product in products.OfType<JsonObject>())
            {
                if (product["description"] is not JsonValue descriptionValue
                    || !descriptionValue.TryGetValue<string>(out var description))
                {
                    continue;
                }

                var limited = LimitText(description);
                if (!string.Equals(limited, description, StringComparison.Ordinal))
                {
                    product["description"] = JsonValue.Create(limited);
                    changed = true;
                }
            }

            if (changed)
            {
                limitedText = root.ToJsonString();
            }

            return changed;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string LimitText(string value)
    {
        if (value.Length <= MaximumDescriptionTextElementCount)
        {
            return value;
        }

        var textElements = new StringInfo(value);
        return textElements.LengthInTextElements <= MaximumDescriptionTextElementCount
            ? value
            : textElements.SubstringByTextElements(0, MaximumDescriptionTextElementCount);
    }
}
