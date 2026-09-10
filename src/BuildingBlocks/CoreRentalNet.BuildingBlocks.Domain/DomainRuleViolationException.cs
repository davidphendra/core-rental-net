namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>Raised when an attempt is made to put a domain object into an invalid state.</summary>
public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException()
    {
    }

    public DomainRuleViolationException(string message)
        : base(message)
    {
    }

    public DomainRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
