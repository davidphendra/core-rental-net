using CoreRentalNet.Modules.Discovery.Application.Indexing;

namespace CoreRentalNet.Host.Tests;

/// <summary>A freshness check a test decides the answer to.</summary>
internal sealed class StubCatalogIndexFreshnessService(Func<CatalogIndexVerdict> verdict) : ICatalogIndexFreshnessService
{
    /// <summary>How many times the check ran, so a test can prove it was skipped rather than passed.</summary>
    public int Checks { get; private set; }

    public CatalogIndexVerdict Check(string catalogueHash, string modelId, int width, string composition)
    {
        Checks++;

        return verdict();
    }
}
