namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>The intent table could not be read, or says something this vocabulary does not have.</summary>
/// <remarks>
/// Loud and specific, like the catalogue loader: a table that cannot be trusted is a table that would
/// silently send every request down the inferred path, and a failure naming the file and the row is
/// what stops that being discovered by a customer.
/// </remarks>
public sealed class IntentTableException : Exception
{
    public IntentTableException(string message)
        : base(message)
    {
    }

    public IntentTableException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
