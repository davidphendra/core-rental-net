using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>Fails a stage's model call whose answer repeats the caller's catalogue token.</summary>
/// <remarks>
/// <para>
/// The token is read off the invocation and never enters a message, so a model that can echo it has seen it -
/// and a token in a model's answer is a token on its way to a customer. This is the one guardrail worth failing
/// a run for: it cannot fire on a well-behaved model, and it is the difference between a leak and a failed run.
/// </para>
/// <para>
/// It checks the caller's own token and nothing else, because everything else a model may write is either
/// already the customer's to see or the application's to hygienize.
/// </para>
/// </remarks>
internal sealed class GuardrailChatClient(
    IChatClient innerClient,
    IMcpAccessTokenService accessTokens) : DelegatingChatClient(innerClient)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);

        Guard(response.Text);

        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            Guard(update.Text);

            yield return update;
        }
    }

    /// <summary>Throws when the caller's token survived into an answer.</summary>
    private void Guard(string? modelOutputText)
    {
        var callerCatalogueToken = accessTokens.Token;

        if (string.IsNullOrEmpty(callerCatalogueToken) || string.IsNullOrEmpty(modelOutputText))
        {
            return;
        }

        if (modelOutputText.Contains(callerCatalogueToken, StringComparison.Ordinal))
        {
            // Mark the span being recorded before the throw, because content capture means the answer may already
            // be on it and a throw cannot unsend an attribute. The redaction processor reads the mark at export.
            Activity.Current?.SetTag(WorkspaceTelemetry.TokenLeakDetected, true);
            WorkspaceTelemetry.GuardrailTokenLeak.Add(1);

            throw new CallerTokenLeakException(
                "A stage agent's answer carried the caller's catalogue token, so the run was stopped before the " +
                "answer could reach the customer.");
        }
    }
}
