namespace CoreRentalNet.Modules.Workspace.Application.Contracts;

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

public interface IConvertWorkspaceToOrder
{
    /// <summary>Reads the draft as it stands, changing nothing.</summary>
    Task<WorkspaceConversion> DescribeAsync(string rawDraftToken, CancellationToken cancellationToken = default);

    /// <summary>Makes the draft terminal. Idempotent: the second call reports what the first did.</summary>
    Task<WorkspaceConversion> ConvertAsync(string rawDraftToken, CancellationToken cancellationToken = default);
}
