using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Guardrails.Tools;
using CoreRentalNet.Agents.Shared.Mcp;

namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>Applies the call's own credential, and the tools it entitles, at the model call.</summary>
/// <remarks>
/// <para>
/// The served agent has to be the workflow agent itself. Hosting redirects a hosted workflow's checkpoints to its
/// durable store only by copying the workflow agent, and it declines to copy one behind middleware — copying only
/// the innermost agent would throw the wrapper away. So the per-call work lives here instead, one decorator per
/// stage agent, wrapped around the chat client that agent runs on.
/// </para>
/// <para>
/// <b>Nothing is lifted out of a message.</b> The caller's token arrives on the invocation and is read once, at
/// the start of the run, into the call-scoped <see cref="IMcpAccessTokenService"/> — so every stage shares the one
/// token and no message ever carried it. What is left for this decorator is the decision it is named for: which
/// tools this call is offered. A stage that reads the server is offered the catalogue's, each wrapped in the
/// guardrail pipeline, so the tool that runs is the tool that was guarded; a stage that does not is offered none.
/// </para>
/// <para>
/// <b>Not generic any more, and that is the point.</b> The type parameter existed only to name the request whose
/// token the splitter had to recognise, so removing the message carrier removed the parameter with it.
/// </para>
/// </remarks>
internal sealed class AuthorisedMcpChatClient(
    IChatClient innerClient,
    IMcpAccessTokenService accessTokens,
    IMcpAuthorizationConnection mcpAuthorizationConnection,
    bool offersMcpTools,
    IToolAllowList toolAllowList,
    IToolGuardPipeline toolGuardPipeline,
    ILogger<AuthorisedMcpChatClient> logger) : DelegatingChatClient(innerClient)
{
    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var (visibleMessages, optionsWithTools) = await PrepareAsync(messages, options, cancellationToken);

        return await base.GetResponseAsync(visibleMessages, optionsWithTools, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (visibleMessages, optionsWithTools) = await PrepareAsync(messages, options, cancellationToken);

        await foreach (var update in base.GetStreamingResponseAsync(visibleMessages, optionsWithTools, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>The messages the model may see, and the options it may reach the tools through.</summary>
    private async Task<(IEnumerable<ChatMessage> Messages, ChatOptions? Options)> PrepareAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        // The messages are the model's to read as they are: no member of one is a secret any more, because the
        // token is read from the invocation and never placed in a message.

        // A stage that is not a catalogue reader is offered no tools at all — not "whatever the options it was
        // handed happened to carry".
        if (!offersMcpTools)
        {
            logger.LogInformation("Chat call prepared: the stage is offered no MCP tools.");

            return (messages, WithoutTools(options));
        }

        // A catalogue reader with nothing to search with is offered no tools either: no connection to list them
        // from, or no caller token to search with.
        if (!mcpAuthorizationConnection.IsConfigured || string.IsNullOrEmpty(accessTokens.Token))
        {
            logger.LogInformation(
                "Chat call prepared: no MCP tools; caller token {Token}.",
                string.IsNullOrEmpty(accessTokens.Token) ? "absent" : "present");

            return (messages, WithoutTools(options));
        }

        // The allow-list filters what the model is offered, not only what may run: a tool the deployment does not
        // permit should never reach the prompt, where its name and description are untrusted input (§12). Each
        // permitted tool goes on wrapped, so guarding and recording happen wherever the tool runs rather than
        // only where the framework happened to look.
        var tools = (await mcpAuthorizationConnection.ToolsAsync(cancellationToken))
            .Where(tool => toolAllowList.Contains(tool.Name))
            .Select(tool => tool is AIFunction toolFunction
                ? (AITool)new GuardedToolFunction(toolFunction, toolGuardPipeline)
                : tool)
            .ToArray();

        logger.LogInformation("Chat call prepared: {Count} MCP tool(s); caller token present.", tools.Length);

        // The options the stage was handed may be shared with another stage, so the tools go on a copy rather
        // than on the caller's instance.
        var optionsWithTools = options?.Clone() ?? new ChatOptions();
        var allTools = optionsWithTools.Tools is { } existingTools ? new List<AITool>(existingTools) : [];
        allTools.AddRange(tools);
        optionsWithTools.Tools = allTools;

        return (messages, optionsWithTools);
    }

    /// <summary>The options a stage with no catalogue tools runs on: the caller's, with no tool offered.</summary>
    private static ChatOptions WithoutTools(ChatOptions? options)
    {
        var withoutTools = options?.Clone() ?? new ChatOptions();
        withoutTools.Tools = [];

        return withoutTools;
    }
}
