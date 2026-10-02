using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Wire;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Routing;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Serialization;
using CoreRentalNet.Agents.Shared.Workflows;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>Turns what the caller sent into the run's state, lifting the token out of it first.</summary>
/// <remarks>
/// <para>
/// The start of the graph, and a chat-protocol executor on purpose: hosting serves a workflow as an agent only
/// when the workflow accepts chat messages <b>and</b> a turn token, so a start executor that took a typed
/// message would be refused before a run began.
/// </para>
/// <para>
/// The caller's catalogue token is taken out of the message here and kept in the request-scoped service the
/// catalogue tools present, so no stage agent is ever shown it.
/// </para>
/// </remarks>
internal sealed class WorkspaceInputExecutor(
    IMcpAccessTokenService accessTokens,
    AccessTokenHeaderReader accessTokenHeaderReader,
    WorkspaceSuggestionWorkflowOptions workflowOptions,
    ILogger logger)
    : ChatEntryStageExecutor(WorkspaceWorkflowExecutorNames.Input)
{
    /// <summary>The currency a console sentence is composed in, which is the one the catalogue is priced in.</summary>
    private const string ComposedCurrency = "IDR";

    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
        => base.ConfigureProtocol(protocolBuilder).SendsMessageTypes([typeof(WorkspaceSuggestionWorkflowState)]);

    protected override async ValueTask TakeTurnAsync(
        List<ChatMessage> messages,
        IWorkflowContext context,
        bool? emitEvents,
        CancellationToken cancellationToken = default)
    {
        // Read here, once, because this is the only stage still inside the HTTP request: the workflow may hand a
        // later stage to another thread, and a header read there would depend on the context surviving the hop.
        // The message carries nothing to take back out, so the run's token comes from one place and one only.
        accessTokens.Token ??= accessTokenHeaderReader.ReadTheCallersCatalogueAccessToken();

        var suggestionRequest = Read(messages) ?? FromSentence(messages);

        // The entry node writes its own line, because it speaks the chat protocol through its own base rather
        // than the workspace one. Its result is the request it read: never the token, which was lifted above.
        logger.LogInformation(
            "Node {Node} completed. Result: {Result}",
            Id,
            ContractJson.Serialize(suggestionRequest));

        var workspaceSuggestionWorkflowState = new WorkspaceSuggestionWorkflowState
        {
            CustomerWorkflowIdentifier = suggestionRequest.RunId,
            OriginalCustomerQuery = suggestionRequest.Query,
            SlotCapacityRules = suggestionRequest.Slots,
            CatalogueCurrency = suggestionRequest.Currency,
            CustomerStatedCeilingMonthly = suggestionRequest.CeilingMonthly,
            MaximumAttemptCount = workflowOptions.MaximumAttemptCount,
        };

        await context.SendMessageAsync(workspaceSuggestionWorkflowState, targetId: null, cancellationToken);
    }

    /// <summary>The application's request, when the messages carry one.</summary>
    private static SuggestionRequest? Read(IReadOnlyList<ChatMessage> visibleMessages)
    {
        foreach (var message in visibleMessages)
        {
            if (message.Role != ChatRole.User || string.IsNullOrWhiteSpace(message.Text))
            {
                continue;
            }

            try
            {
                if (System.Text.Json.JsonSerializer.Deserialize<SuggestionRequest>(
                        message.Text, ContractJson.Options) is { } suggestionRequest)
                {
                    return suggestionRequest;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Not the request. Keep looking; a later message may be.
            }
        }

        return null;
    }

    /// <summary>The sentence a console sent, read as a query rather than refused.</summary>
    /// <remarks>
    /// <para>
    /// The application always sends the full request — the sentence, the slot rules and the caller's token. A
    /// console sends only the sentence, and refusing to run without the envelope would make the agent
    /// untestable from <c>azd ai agent invoke</c>, which is the whole point of being able to address it.
    /// </para>
    /// <para>
    /// So a bare sentence is read as a query over every slot this deployment can compose for, at capacity one,
    /// with no ceiling. That is deliberately permissive: a console run is a smoke test of hosting and the
    /// pipeline, and a request that states a ceiling or a capacity is the application's to send.
    /// </para>
    /// </remarks>
    private static SuggestionRequest FromSentence(IReadOnlyList<ChatMessage> visibleMessages)
    {
        var sentence = visibleMessages
            .LastOrDefault(message => message.Role == ChatRole.User && !string.IsNullOrWhiteSpace(message.Text))
            ?.Text
            ?? throw new InvalidOperationException(
                "The run was started with nothing to read: a workspace suggestion run needs either the " +
                "application's request or a sentence to compose for.");

        return new SuggestionRequest(
            RunId: Guid.NewGuid().ToString("n"),
            Query: sentence,
            Currency: ComposedCurrency,
            CeilingMonthly: null,
            Slots: [.. Enum.GetValues<WorkspaceSlot>().Select(slot => new SlotRule(slot, 1))]);
    }
}
