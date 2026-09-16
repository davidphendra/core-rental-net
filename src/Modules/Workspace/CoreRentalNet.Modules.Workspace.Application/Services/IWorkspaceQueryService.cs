using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>
/// The plain, rule-free projections over a workspace record. Split from the change
/// service so a reader gets the queries without the write rules, and the other way round.
/// </summary>
public interface IWorkspaceQueryService
{
    IReadOnlyList<SlotAssignment> AssignmentsFor(Domain.Workspace workspace, SlotId slot);

    int TotalUnits(Domain.Workspace workspace);

    bool IsEmpty(Domain.Workspace workspace);

    bool IsConverted(Domain.Workspace workspace);

    /// <summary>Whether every slot that must hold something does.</summary>
    bool HasEveryMandatorySlot(Domain.Workspace workspace);
}
