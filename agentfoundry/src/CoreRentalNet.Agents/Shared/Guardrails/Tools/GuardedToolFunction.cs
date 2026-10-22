using System.Diagnostics;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Telemetry;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Guardrails.Tools;

/// <summary>One tool, run through the guardrail pipeline on the way in and on the way back.</summary>
/// <remarks>
/// <para>
/// <b>It wraps the tool rather than the function loop.</b> The catalogue's tools reach the model's options from
/// inside the chat-client chain, downstream of where the agent's function middleware transforms tools — so a tool
/// guarded there is a tool guarded nowhere. Wrapping the tool itself means the pipeline runs wherever the tool
/// was attached, and the ledger is written by the one path that also guards what the model is offered.
/// </para>
/// <para>
/// Everything a model may call stays the tool's own: the name, the description and the schema are delegated, so
/// wrapping a tool cannot change what a model may call.
/// </para>
/// </remarks>
internal sealed class GuardedToolFunction(
    AIFunction catalogueTool,
    IToolGuardPipeline catalogueGuardPipeline,
    IMcpAccessTokenService? accessTokenService) : DelegatingAIFunction(catalogueTool)
{
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        // The token is ambient only while this catalogue tool is running. The lease restores the previous
        // context before control returns to the function loop.
        using var tokenScope = McpRunScope.Enter(accessTokenService?.Token);

        var toolInvocation = new ToolInvocation(Name, arguments);
        var runId = Activity.Current?.GetTagItem(WorkspaceTelemetry.RunId) as string;
        using var toolActivity = WorkspaceTelemetry.ActivitySource.StartActivity("tool.invoke");
        toolActivity?.SetTag(WorkspaceTelemetry.ToolName, Name);
        toolActivity?.SetTag(WorkspaceTelemetry.RunId, runId);

        var guardrailDecision = await catalogueGuardPipeline.BeforeAsync(toolInvocation, cancellationToken);

        if (!guardrailDecision.Allowed)
        {
            // Returned rather than thrown: the function loop hands the reason back to the model, which may
            // correct the call. A throw would fail the stage and give the model no chance to.
            return guardrailDecision.Reason ?? "The call was refused.";
        }

        var toolResult = await base.InvokeCoreAsync(arguments, cancellationToken);

        var validatedToolResult = await catalogueGuardPipeline.AfterAsync(
            toolInvocation,
            new UntrustedToolResult(toolInvocation.ToolName, toolResult),
            cancellationToken);

        return validatedToolResult.Value;
    }
}
