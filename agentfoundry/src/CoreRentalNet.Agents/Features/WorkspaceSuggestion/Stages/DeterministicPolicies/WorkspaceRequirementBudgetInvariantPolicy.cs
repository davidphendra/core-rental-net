using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Keeps a requirement expansion's budgets on the amounts the request actually supplied.</summary>
/// <remarks>
/// <para>
/// The rephraser is told not to invent a total when neither the sentence nor the application supplied one. A
/// model can still copy a figure from a retry correction, and that figure is evidence about the cheapest product,
/// not a budget. This policy makes the rule true after the model has answered.
/// </para>
/// <para>
/// <b>It only removes money; it never invents it.</b> When no total exists every component amount becomes null.
/// When the application recorded a ceiling and the model left the total null, the ceiling becomes the total. A
/// model's stated or derived amounts are otherwise left alone.
/// </para>
/// </remarks>
public sealed class WorkspaceRequirementBudgetInvariantPolicy
{
    /// <summary>The same expansion, with every amount grounded in the request's own budget.</summary>
    public WorkspaceRequirementExpansion ApplyBudgetInvariant(
        WorkspaceRequirementExpansion requirementExpansion,
        int? applicationRecordedCeilingMonthly)
    {
        ArgumentNullException.ThrowIfNull(requirementExpansion);

        var effectiveTotal = CustomerTotal(requirementExpansion, applicationRecordedCeilingMonthly);

        if (effectiveTotal is null)
        {
            return WithNoBudget(requirementExpansion);
        }

        var total = requirementExpansion.TotalMonthlyBudget;

        return requirementExpansion with
        {
            TotalMonthlyBudget = total with
            {
                Amount = effectiveTotal,
                IsExplicit = true,
            },
        };
    }

    private static int? CustomerTotal(
        WorkspaceRequirementExpansion requirementExpansion,
        int? applicationRecordedCeilingMonthly)
    {
        if (WorkspaceCustomerBudgetReader.TryRead(requirementExpansion.OriginalCustomerQuery, out var sentenceTotal))
        {
            return sentenceTotal;
        }

        return applicationRecordedCeilingMonthly;
    }

    private static WorkspaceRequirementExpansion WithNoBudget(WorkspaceRequirementExpansion requirementExpansion)
    {
        var components = requirementExpansion.ComponentExpansions;

        return requirementExpansion with
        {
            TotalMonthlyBudget = requirementExpansion.TotalMonthlyBudget with
            {
                Amount = null,
                IsExplicit = false,
            },
            ComponentExpansions = new WorkspaceComponentExpansions(
                Desk: WithoutBudget(components.Desk),
                Chair: WithoutBudget(components.Chair),
                Monitor: WithoutBudget(components.Monitor),
                Lamp: WithoutBudget(components.Lamp),
                Plant: WithoutBudget(components.Plant),
                BeanBag: WithoutBudget(components.BeanBag),
                CoffeeMachine: WithoutBudget(components.CoffeeMachine)),
        };
    }

    private static WorkspaceComponentExpansion WithoutBudget(WorkspaceComponentExpansion componentExpansion)
        => componentExpansion with
        {
            MonthlyBudget = componentExpansion.MonthlyBudget with
            {
                MaximumAmount = null,
                IsExplicit = false,
                IsDerived = false,
            },
        };
}
