namespace CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;

/// <summary>Replace this workspace's slots with a composition, if it has not moved on.</summary>
/// <remarks>
/// <para>
/// The second constructor argument is what makes this safe to offer from a page that may have been open
/// for minutes: the composition was chosen against a workspace at a version, and if the workspace has
/// moved past it the write is refused rather than overwriting whatever the other tab did. Last write
/// winning would be a silent lost update, which is the one thing the draft's version exists to prevent.
/// </para>
/// <para>
/// The delivery address is deliberately absent. This operation replaces the slots; leaving the address
/// out of the payload is what makes it impossible for one to change the other, rather than a rule
/// somebody has to remember.
/// </para>
/// </remarks>
public sealed record ApplyCompositionCommand(
    string DraftToken,
    int ExpectedVersion,
    IReadOnlyList<CompositionLine> Lines);
