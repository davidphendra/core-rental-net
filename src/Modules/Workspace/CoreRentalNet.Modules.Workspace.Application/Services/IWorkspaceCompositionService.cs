using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>
/// Replacing everything a draft holds, in one act.
/// </summary>
/// <remarks>
/// A service of its own rather than a sixth method on <see cref="IWorkspaceService"/>, because it answers a
/// different question. Everything else there changes one thing about a workspace - a product in a slot, a
/// quantity, an address - and this replaces the whole of what it holds. Keeping them together would also put
/// the file past the line budget this repository enforces by test.
/// </remarks>
public interface IWorkspaceCompositionService
{
    /// <summary>
    /// Replaces the draft's assignments with <paramref name="composition"/>, and touches nothing else.
    /// </summary>
    /// <param name="expectedVersion">
    /// The version the caller read before deciding what to replace it with. A different one means the
    /// workspace moved underneath them, and nothing is applied.
    /// </param>
    void ReplaceComposition(
        Domain.Workspace workspace,
        IReadOnlyList<SlotAssignment> composition,
        int expectedVersion);
}
