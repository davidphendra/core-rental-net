using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Tools;

namespace WorkspaceSuggestions.Workflows;

/// <summary>Holds this call's MCP token, discovers the tools it entitles, and runs the pipeline with them.</summary>
/// <remarks>
/// <para>
/// The tools are per caller, so the pipeline is per caller: each run builds its own with the tools its own
/// token returned. Nothing about a run survives the call.
/// </para>
/// <para>
/// Its base agent is the pipeline built with no tools — an identity and a session handler, never run. Every run
/// builds and uses its own.
/// </para>
/// </remarks>
internal sealed class McpToolsAccessAgent<TRequest>(
    IWorkspaceWorkflow workflow,
    CatalogToolSettings catalogSettings,
    IMcpAccessTokenService mcpAccessTokenService,
    IMcpToolRequest<TRequest> request) : DelegatingAIAgent(workflow.AsAIAgent([]))
{
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var (settable, token) = request.Split(messages);

        // From here to the end of the call, every MCP request the transport makes carries this token.
        mcpAccessTokenService.Token = token;

        return await workflow
            .AsAIAgent(await ToolsAsync(cancellationToken))
            .RunAsync(settable, session, options, cancellationToken);
    }

    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (settable, token) = request.Split(messages);

        mcpAccessTokenService.Token = token;

        await foreach (var update in workflow
            .AsAIAgent(await ToolsAsync(cancellationToken))
            .RunStreamingAsync(settable, session, options, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>The catalogue tools this call may use, discovered with this call's token.</summary>
    private async Task<IReadOnlyList<AITool>> ToolsAsync(CancellationToken cancellationToken)
    {
        if (!catalogSettings.IsConfigured)
        {
            return [];
        }

        await using var mcpClient = await McpAuthenticationHelper.ConnectAsync(
            new Uri(catalogSettings.McpEndpoint, UriKind.Absolute),
            mcpAccessTokenService,
            cancellationToken);

        return mcpClient is null
            ? []
            : [.. (await mcpClient.ListToolsAsync(cancellationToken: cancellationToken)).Cast<AITool>()];
    }
}
