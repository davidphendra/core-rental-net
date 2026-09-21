using CoreRentalNet.Modules.Discovery.Application.Indexing;

namespace CoreRentalNet.Host.Tests;

/// <summary>A freshness check a test decides the answer to.</summary>
internal sealed class StubCatalogIndexFreshness(Func<CatalogIndexVerdict> verdict) : ICatalogIndexFreshness
{
    /// <summary>How many times the check ran, so a test can prove it was skipped rather than passed.</summary>
    public int Checks { get; private set; }

    public Task<CatalogIndexVerdict> CheckAsync(
        string catalogueHash,
        string modelId,
        int width,
        CancellationToken cancellationToken)
    {
        Checks++;

        return Task.FromResult(verdict());
    }
}
