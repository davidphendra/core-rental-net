using CoreRentalNet.Modules.Rentals.Domain;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

internal sealed class InMemoryRentalRepository : IRentalRepository
{
    private readonly List<Domain.Rental> rentals = [];

    public IReadOnlyList<Domain.Rental> All => rentals;

    public Task<Domain.Rental?> FindByTokenAsync(AccessToken token, CancellationToken cancellationToken = default)
        => Task.FromResult(rentals.FirstOrDefault(rental => rental.AccessTokenHash == token.Hash));

    public Task<Domain.Rental?> FindByIdAsync(RentalId id, CancellationToken cancellationToken = default)
        => Task.FromResult(rentals.FirstOrDefault(rental => rental.Id == id));

    public Task AddAsync(Domain.Rental rental, CancellationToken cancellationToken = default)
    {
        rentals.Add(rental);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly List<Invoice> invoices = [];

    public IReadOnlyList<Invoice> All => invoices;

    public Task<IReadOnlyList<Invoice>> ListForRentalAsync(RentalId rentalId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Invoice>>(invoices.Where(invoice => invoice.RentalId == rentalId).ToArray());

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        invoices.Add(invoice);
        return Task.CompletedTask;
    }
}

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

internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
