namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// Raised when a use case is asked about something that does not exist. Distinct from a
/// domain rule violation: nothing was invalid, the thing simply is not there.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
