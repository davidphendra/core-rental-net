
using CoreRentalNet.Modules.Rentals.Domain.Persistence;

namespace CoreRentalNet.Modules.Rentals.UnitTests;

internal sealed class RecordingUnitOfWork : IUnitOfWork
{
    public int Saves { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Saves++;
        return Task.CompletedTask;
    }
}
