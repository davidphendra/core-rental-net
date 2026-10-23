using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>Splits a tool-calling turn from the structured answer that follows it.</summary>
/// <remarks>
/// <para>
/// Some local model servers reject a request that carries both tools and a JSON-schema response format: the
/// tool-call grammar and the response grammar cannot be combined, so the request fails before the model runs.
/// This decorator uses one shape per phase. The first call carries the tools without the format; the call that
/// follows the tool results carries the format without the tools.
/// </para>
/// <para>
/// It sits inside the function-invoking client, because that is the client that makes the follow-up call with
/// the tool results. It is used only for a stage whose profile offers catalogue tools.
/// </para>
/// </remarks>
internal sealed class ToolThenJsonChatClient(
    IChatClient innerClient,
    ChatResponseFormat structuredOutput,
    ILogger<ToolThenJsonChatClient> logger) : DelegatingChatClient(innerClient)
{
    private readonly ChatResponseFormat _structuredOutput = structuredOutput
        ?? throw new ArgumentNullException(nameof(structuredOutput));

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var (preparedMessages, preparedOptions) = Prepare(messages, options);

        return await base.GetResponseAsync(
            preparedMessages,
            preparedOptions,
            cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (preparedMessages, preparedOptions) = Prepare(messages, options);

        await foreach (var update in base.GetStreamingResponseAsync(
            preparedMessages,
            preparedOptions,
            cancellationToken))
        {
            yield return update;
        }
    }

    private (IReadOnlyList<ChatMessage> Messages, ChatOptions Options) Prepare(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options)
    {
        var messageList = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();

        var hasToolResults = messageList
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Any();

        var prepared = options?.Clone() ?? new ChatOptions();
        var hasTools = prepared.Tools is { Count: > 0 };

        if (hasToolResults)
        {
            // The tools have done their work. Ask for the contract without offering tools: this is the
            // combination the local server accepts.
            prepared.Tools = [];
            prepared.ToolMode = null;
            prepared.ResponseFormat = _structuredOutput;
            prepared.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            prepared.AdditionalProperties[ChatCallPhase.Key] = ChatCallPhase.FinalStructured;

            logger.LogInformation(
                "Tool results present; structured output requested without catalogue tools.");
        }
        else if (hasTools)
        {
            // The first turn is a tool-calling turn. Withhold the response grammar until the follow-up call.
            prepared.ResponseFormat = null;
            prepared.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            prepared.AdditionalProperties[ChatCallPhase.Key] = ChatCallPhase.ToolPlanning;

            logger.LogInformation(
                "Tool-calling turn; structured output withheld until tool results arrive.");
        }

        return (messageList, prepared);
    }
}
