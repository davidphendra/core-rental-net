using CoreRentalNet.Modules.Discovery.Application.Selection;

namespace CoreRentalNet.Host.Tests;

/// <summary>An offer signal a test can read, and can make fail.</summary>
internal sealed class RecordingOffers(bool fail = false) : IOfferSelection
{
    /// <summary>Every batch of SKUs this signal was asked to record, in order.</summary>
    public List<IReadOnlyList<string>> Offered { get; } = [];

    public Task<int> OfferAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken)
    {
        if (fail)
        {
            throw new InvalidOperationException("the signal could not be written");
        }

        Offered.Add(skus);

        return Task.FromResult(skus.Count);
    }
}
