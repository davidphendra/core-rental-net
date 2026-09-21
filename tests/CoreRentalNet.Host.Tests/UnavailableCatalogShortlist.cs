using CoreRentalNet.Modules.Discovery.Application.Shortlist;

namespace CoreRentalNet.Host.Tests;

/// <summary>A retrieval that could not be made, so a test can hold the run's answer to it.</summary>
internal sealed class UnavailableCatalogShortlist : ICatalogShortlist
{
    public Task<IReadOnlyList<ShortlistItem>> ForAsync(string query, CancellationToken cancellationToken)
        => Task.FromException<IReadOnlyList<ShortlistItem>>(
            new ShortlistUnavailableException("the embedding deployment did not answer"));
}
