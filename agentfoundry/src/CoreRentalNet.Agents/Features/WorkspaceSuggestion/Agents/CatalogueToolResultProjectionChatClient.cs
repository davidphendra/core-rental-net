using System.Text.Json;
using System.Text.Json.Nodes;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Model;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;

/// <summary>Gives the retriever's final model call a compact view of the catalogue answers.</summary>
/// <remarks>
/// <para>
/// The retriever only reports what each search found; it does not compose from the rows. The rows are already in
/// the tool ledger for the reranker, so sending every product back to the retriever is prompt cost without a
/// consumer. This projection keeps the counts, totals, truncation flag, and cheapest excluded price.
/// </para>
/// <para>
/// It sits inside the evaluation trace: the trace still records the tool answers before this projection, so
/// retrieval evidence remains complete while the model receives the smaller prompt.
/// </para>
/// </remarks>
internal sealed class CatalogueToolResultProjectionChatClient(
    ITelemetryChatClient innerClient) : DelegatingChatClient(innerClient), ITelemetryChatClient
{
    public AgentRunUsage Total => innerClient.Total;

    public void StartRun(string runId) => innerClient.StartRun(runId);

    public void CompleteRun(string runStatus) => innerClient.CompleteRun(runStatus);

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetResponseAsync(Project(messages, options), options, cancellationToken);

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GetStreamingResponseAsync(Project(messages, options), options, cancellationToken);

    private IReadOnlyList<ChatMessage> Project(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options)
    {
        if (options?.AdditionalProperties?.TryGetValue(ChatCallPhase.Key, out var phase) is not true
            || phase is not ChatCallPhase.FinalStructured)
        {
            return messages as IReadOnlyList<ChatMessage> ?? messages.ToArray();
        }

        return [.. messages.Select(ProjectMessage)];
    }

    private ChatMessage ProjectMessage(ChatMessage message)
    {
        if (!message.Contents.OfType<FunctionResultContent>().Any())
        {
            return message;
        }

        var contents = message.Contents
            .Select(content =>
            {
                if (content is not FunctionResultContent result || result.Result is not { } value)
                {
                    return content;
                }

                return TrySummary(value, out var summary)
                    ? new FunctionResultContent(result.CallId, summary)
                    : content;
            })
            .ToArray();

        return new ChatMessage(message.Role, contents);
    }

    private static bool TrySummary(object value, out string summary)
    {
        summary = string.Empty;
        var text = value switch
        {
            string stringValue => stringValue,
            TextContent textContent => textContent.Text,
            JsonElement element => element.GetRawText(),
            JsonDocument document => document.RootElement.GetRawText(),
            JsonNode node => node.ToJsonString(),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            if (!root.TryGetProperty("matches", out var matches)
                || !matches.TryGetProperty("count", out var count)
                || !matches.TryGetProperty("total", out var total))
            {
                return false;
            }

            var truncated = matches.TryGetProperty("truncated", out var truncatedElement)
                && truncatedElement.GetBoolean();
            var cheapest = root.TryGetProperty("cheapestProductIgnoringTheCeiling", out var cheapestElement)
                && cheapestElement.ValueKind is not JsonValueKind.Null
                    ? cheapestElement.Clone()
                    : (JsonElement?)null;

            summary = JsonSerializer.Serialize(new
            {
                matches = new { count = count.GetInt32(), total = total.GetInt32(), truncated },
                cheapestProductIgnoringTheCeiling = cheapest,
            });

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
