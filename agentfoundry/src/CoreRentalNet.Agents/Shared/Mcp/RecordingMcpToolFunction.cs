using System.Text.Json;
using Microsoft.Extensions.AI;

namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>One MCP tool, recorded on the way back to the model that called it.</summary>
/// <remarks>
/// <para>
/// <b>It records the answer rather than the call, and before the model sees it.</b> The function loop hands a
/// result straight back to the model, so a decorator placed above the loop would see a conversation rather than a
/// tool's answer, and one placed after it would record nothing if the model failed on the way. This is the one
/// place where a tool's answer and the arguments it was asked with are both in hand.
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
        var toolAnswer = await toolFunction.InvokeAsync(toolArguments, cancellationToken).ConfigureAwait(false);

        recordedToolAnswers.Record(toolFunction.Name, toolArguments, toolAnswer?.ToString() ?? string.Empty);

        return toolAnswer;
    }
}
