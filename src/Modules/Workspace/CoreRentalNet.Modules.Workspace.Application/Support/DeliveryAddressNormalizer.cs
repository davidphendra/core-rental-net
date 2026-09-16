namespace CoreRentalNet.Modules.Workspace.Application.Support;

/// <summary>
/// Turns a typed address into the value that is stored: each line trimmed, blank lines dropped and
/// line endings normalised.
/// </summary>
/// <remarks>
/// A pure transformation, so it is a static helper rather than a stateful service. The
/// bounds on the result are enforced by <see cref="WorkspaceService.SetDeliveryAddress"/>.
/// </remarks>
internal static class DeliveryAddressNormalizer
{
    public static string? Normalize(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var lines = address
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);

        var normalized = string.Join('\n', lines).Trim();

        return normalized.Length == 0 ? null : normalized;
    }
}
