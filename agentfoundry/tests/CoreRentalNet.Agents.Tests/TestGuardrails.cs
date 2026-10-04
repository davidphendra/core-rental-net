using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Guardrails;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Guardrails.MAF;
using CoreRentalNet.Agents.Shared.Guardrails.Pipeline;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A guardrail middleware with no guards, for tests that exercise the stages rather than the policy.</summary>
/// <remarks>
/// The real component is the same adapter; only the guard list is empty, so a stage test runs the production
/// wiring and simply has nothing to refuse.
/// </remarks>
internal static class TestGuardrails
{
    public static IGuardrailFunctionMiddleware Middleware { get; } =
        new GuardrailFunctionMiddleware(new ToolGuardPipeline([], []));

    /// <summary>The catalogue's own allow-list, which is also what the offer filter reads.</summary>
    public static IToolAllowList AllowList { get; } = new CatalogueToolAllowList();
}
