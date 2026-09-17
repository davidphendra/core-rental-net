namespace AgentFoundry.WorkspaceSuggestions.Catalogue;

/// <summary>
/// Reads the catalogue, once per run.
/// </summary>
/// <remarks>
/// A port, because the read is authenticated and over HTTP and the unit tier must run without either.
/// One read per run rather than one per slot: the catalogue is an immutable snapshot, so the slots a
/// slot is filled from are the whole snapshot grouped locally, and the endpoint is asked once.
/// </remarks>
public interface ICatalogueReader
{
    Task<CataloguePage> ReadAsync(CancellationToken cancellationToken = default);
}
