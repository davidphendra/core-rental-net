namespace CoreRentalNet.Modules.Workspace.Application.Contracts.Conversion;

/// <summary>Workspace's published conversion contract; Rentals calls it when a draft becomes an order.</summary>
public interface IConvertWorkspaceToOrder
{
    /// <summary>Reads the draft as it stands, changing nothing.</summary>
    Task<WorkspaceConversion> DescribeAsync(string rawDraftToken, CancellationToken cancellationToken = default);

    /// <summary>Makes the draft terminal. Idempotent: the second call reports what the first did.</summary>
    Task<WorkspaceConversion> ConvertAsync(string rawDraftToken, CancellationToken cancellationToken = default);
}
