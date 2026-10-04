using System.Diagnostics;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Telemetry;

namespace CoreRentalNet.Agents.Shared.Guardrails.Results;

/// <summary>Removes instruction-like text from a tool's answer before any model reads it.</summary>
/// <remarks>
/// <para>
/// A weak control, on purpose: it is hygiene, not a security boundary. The hard boundary is the structure
/// validator's provenance check — a description cannot make the composer name a SKU a search never returned —
/// and a service-level detector (Prompt Shields) is the follow-on if instruction detection must be stronger.
/// </para>
/// <para>
/// The rewrite is recorded, so it is visible rather than silent, and it happens before the answer is written to
/// the ledger so the pool and every later stage read the same vetted text the model saw.
/// </para>
/// </remarks>
internal sealed class UntrustedToolDataGuard : IToolResultGuard
{
    private const string Removed = "[untrusted content removed]";

    private static readonly string[] InstructionMarkers =
    [
        "ignore previous", "ignore all previous", "disregard previous",
        "ignore the above", "system prompt",
    ];

    public ValueTask<object?> InspectAsync(ToolInvocation invocation, object? result, CancellationToken cancellationToken)
    {
        if (result is not string text)
        {
            return ValueTask.FromResult(result);
        }

        var sanitized = text;

        foreach (var marker in InstructionMarkers)
        {
            if (!sanitized.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            WorkspaceTelemetry.CatalogueUntrustedData.Add(
                1, new TagList { { WorkspaceTelemetry.ToolName, invocation.ToolName } });

            sanitized = sanitized.Replace(marker, Removed, StringComparison.OrdinalIgnoreCase);
        }

        return ValueTask.FromResult<object?>(sanitized);
    }
}
