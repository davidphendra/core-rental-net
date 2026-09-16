
using CoreRentalNet.Modules.Rentals.Domain.Numbering;
using CoreRentalNet.Modules.Rentals.Domain.Persistence;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

/// <summary>A counter per kind and year, standing in for the database sequence.</summary>
internal sealed class CountingNumberSequence : INumberSequence
{
    private readonly Dictionary<(SequenceKind Kind, int Year), int> counters = [];

    public int Reservations { get; private set; }

    public Task<int> ReserveNextAsync(SequenceKind kind, int year, CancellationToken cancellationToken = default)
    {
        Reservations++;
        var key = (kind, year);
        counters[key] = counters.TryGetValue(key, out var value) ? value + 1 : 1;

        return Task.FromResult(counters[key]);
    }
}
