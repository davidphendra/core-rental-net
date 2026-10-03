using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>One MCP tool, recorded and timed on the way back to the model that called it.</summary>
/// <remarks>
/// <para>
/// <b>It records the answer rather than the call, and before the model sees it.</b> The function loop hands a
/// result straight back to the model, so a decorator placed above the loop would see a conversation rather than a
/// tool's answer, and one placed after it would record nothing if the model failed on the way. This is the one
/// place where a tool's answer and the arguments it was asked with are both in hand.
/// </para>
/// <para>
/// It is also the only place a tool call is timed: how long the catalogue took is a fact about the round trip,
/// which lives here and not on the run state.
/// </para>
/// <para>
/// Everything else is delegated: the name, the description and the schema a model is offered stay the tool's, so
/// wrapping a tool cannot change what a model may call.
/// </para>
/// </remarks>
internal sealed class RecordingMcpToolFunction(
    AIFunction toolFunction,
    McpToolAnswerLedger recordedToolAnswers) : AIFunction
{
    /// <inheritdoc />
    public override string Name => toolFunction.Name;

    /// <inheritdoc />
    public override string Description => toolFunction.Description;

    /// <inheritdoc />
    public override JsonElement JsonSchema => toolFunction.JsonSchema;

    /// <inheritdoc />
    public override JsonSerializerOptions JsonSerializerOptions => toolFunction.JsonSerializerOptions;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments toolArguments,
        CancellationToken cancellationToken)
    {
        var tags = new TagList { { WorkspaceTelemetry.ToolName, toolFunction.Name } };
        var stopwatch = Stopwatch.StartNew();

        object? toolAnswer;
        try
        {
            toolAnswer = await toolFunction.InvokeAsync(toolArguments, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            WorkspaceTelemetry.CatalogueToolTimeout.Add(1, tags);
            throw;
        }
        catch (Exception)
        {
            WorkspaceTelemetry.CatalogueConnectionFailure.Add(1, tags);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            WorkspaceTelemetry.CatalogueConnectionDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);
        }

        recordedToolAnswers.Record(toolFunction.Name, toolArguments, toolAnswer?.ToString() ?? string.Empty);

        return toolAnswer;
    }
}
