using CoreRentalNet.Modules.Workspace.Application.Rules;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>The plain projections over a workspace record.</summary>
public sealed class WorkspaceQueryService(ISlotRuleProvider slotRules) : IWorkspaceQueryService
{
    public IReadOnlyList<SlotAssignment> AssignmentsFor(Domain.Workspace workspace, SlotId slot)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return [.. workspace.Assignments.Where(assignment => assignment.Slot == slot)];
    }

    public int TotalUnits(Domain.Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return workspace.Assignments.Sum(assignment => assignment.Quantity);
    }

    public bool IsEmpty(Domain.Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return workspace.Assignments.Count == 0;
    }

    public bool IsConverted(Domain.Workspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return workspace.State == DraftState.Converted;
    }

    public bool HasEveryMandatorySlot(Domain.Workspace workspace)
        => slotRules.Mandatory.All(rule => AssignmentsFor(workspace, rule.Slot).Count > 0);
}
