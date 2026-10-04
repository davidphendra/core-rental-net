using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Mcp;

namespace CoreRentalNet.Agents.Shared.Guardrails.Results;

/// <summary>Records the sanitized answer, so the pool, reranker and composer read vetted data too.</summary>
/// <remarks>
/// <b>It runs last in the result pipeline.</b> Recording before the untrusted-data guard would let the removed
/// instruction back into the run through the pool, which is built from these recorded answers — the guard would
/// protect the model and not the run. It is an observer rather than a guard, which is why it returns its input
/// untouched; it lives here because the result pipeline is the one point a tool answer passes through.
/// </remarks>
internal sealed class ToolResultRecordingGuard(McpToolAnswerLedger ledger) : IToolResultGuard
{
    public ValueTask<object?> InspectAsync(ToolInvocation invocation, object? result, CancellationToken cancellationToken)
    {
        ledger.Record(invocation.ToolName, invocation.Arguments, result?.ToString() ?? string.Empty);

        return ValueTask.FromResult(result);
    }
}
