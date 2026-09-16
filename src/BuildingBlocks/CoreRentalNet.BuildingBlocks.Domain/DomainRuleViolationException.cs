namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>Raised when an attempt is made to put a domain object into an invalid state.</summary>
public sealed class DomainRuleViolationException : Exception
{
    /// <summary>Creates the exception with no message, for a caller that reports the broken rule itself.</summary>
    public DomainRuleViolationException()
    {
    }

    /// <summary>Creates the exception with the message that names the rule and the offending value.</summary>
    public DomainRuleViolationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception and keeps the cause that made the state invalid.</summary>
    public DomainRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
