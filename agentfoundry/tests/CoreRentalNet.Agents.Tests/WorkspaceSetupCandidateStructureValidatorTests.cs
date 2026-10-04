using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The structure validator, and the one budget invariant nothing else enforces deterministically.</summary>
public sealed class WorkspaceSetupCandidateStructureValidatorTests
{
    [Fact]
    public void A_setup_whose_total_exceeds_the_ceiling_is_structurally_invalid()
    {
        var valid = Validate(monthlyCeiling: 500_000m, out var violations);

        valid.Should().BeFalse();
        violations.Should().Contain("MONTHLY_CEILING_EXCEEDED");
    }

    [Fact]
    public void A_setup_within_the_ceiling_is_structurally_valid()
    {
        var valid = Validate(monthlyCeiling: 700_000m, out var violations);

        valid.Should().BeTrue();
        violations.Should().NotContain("MONTHLY_CEILING_EXCEEDED");
    }

    [Fact]
    public void With_no_ceiling_stated_the_total_is_not_checked()
    {
        var valid = Validate(monthlyCeiling: null, out var violations);

        valid.Should().BeTrue();
        violations.Should().NotContain("MONTHLY_CEILING_EXCEEDED");
    }

    private static bool Validate(decimal? monthlyCeiling, out IReadOnlyList<string> violations)
    {
        var candidate = new WorkspaceSetupCandidate(
            [new WorkspaceSetupLine(WorkspaceSlot.Desk, "DSK1", "Desk", Quantity: 1, Amount: 600_000m)]);

        return new WorkspaceSetupCandidateStructureValidator().IsStructurallyValid(
            candidate,
            WorkspaceRequirementExpansionFixtures.Valid(),
            [new SlotRule(WorkspaceSlot.Desk, 1), new SlotRule(WorkspaceSlot.Chair, 1)],
            [new SelectedWorkspaceComponentProduct(
                new RetrievedWorkspaceComponentProduct(WorkspaceSlot.Desk, "DSK1", "Desk", "description", 600_000m),
                ProductRelevanceLevel.High,
                "why")],
            monthlyCeiling,
            out violations);
    }
}
