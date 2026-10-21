namespace CoreRentalNet.Evaluation.Execution;

/// <summary>The token or the MCP tool catalogue failed authentication before any case ran.</summary>
public sealed class AuthenticationPreflightException : Exception
{
    public AuthenticationPreflightException(string message)
        : base(message)
    {
    }

    public AuthenticationPreflightException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
