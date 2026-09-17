namespace AgentFoundry.WorkspaceSuggestions.Catalogue;

/// <summary>
/// The catalogue could not supply a complete candidate set, so no candidate can be composed honestly.
/// </summary>
/// <remarks>
/// Not a customer outcome: there is no status for it, because the fault is not the request. A truncated
/// page and a slot with nothing in it are the same thing to a caller - the tiers would be computed from
/// a set that is not the set - and a run that composed options anyway would produce three candidates
/// that look right and are not. Failing loudly is the only honest answer available.
/// </remarks>
public sealed class IncompleteCatalogueException : Exception
{
    public IncompleteCatalogueException(string message)
        : base(message)
    {
    }
}
