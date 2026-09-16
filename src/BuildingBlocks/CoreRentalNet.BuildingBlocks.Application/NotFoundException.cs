namespace CoreRentalNet.BuildingBlocks.Application;

/// <summary>
/// Raised when a use case is asked about something that does not exist. Distinct from a
/// domain rule violation: nothing was invalid, the thing simply is not there.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>Creates the exception with no message, for a caller that reports the name of the missing thing itself.</summary>
    public NotFoundException()
    {
    }

    /// <summary>Creates the exception with the message a caller may show.</summary>
    public NotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception and keeps the cause that left the thing missing.</summary>
    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
