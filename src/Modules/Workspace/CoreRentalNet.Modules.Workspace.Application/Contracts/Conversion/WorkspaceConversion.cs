using CoreRentalNet.Modules.Workspace.Application.Contracts.Composition;

namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Conversion;

/// <summary>
/// The result of asking a draft to become an order.
/// </summary>
/// <remarks>
/// Conversion is terminal and idempotent: the second caller gets the same workspace identifier
/// with <see cref="WasAlreadyConverted"/> set, rather than an error. That flag is what makes
/// checkout safe to repeat (matrix CO-11).
/// </remarks>
public sealed record WorkspaceConversion(
    Guid WorkspaceId,
    IReadOnlyList<WorkspaceCompositionLine> Lines,
    string? DeliveryAddress,
    bool WasAlreadyConverted);
