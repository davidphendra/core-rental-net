namespace CoreRentalNet.Modules.Discovery.Application.Ingestion;

/// <summary>
/// Builds the index: it turns the catalogue into vectors and writes them with the recipe that produced them.
/// </summary>
/// <remarks>
/// <b>It replaces, and it refuses to write half.</b> A run either writes every vector and the recipe that
/// describes them, or it writes nothing at all — because an index holding some of a catalogue is worse than
/// no index, and a recipe that describes rows it does not have is worse still.
/// </remarks>
public interface ICatalogIngestion
{
    /// <summary>Writes the index for one catalogue, replacing whatever was there.</summary>
    /// <returns>How many vectors were written.</returns>
    Task<int> IngestAsync(CatalogIngestionRequest request, CancellationToken cancellationToken);
}
