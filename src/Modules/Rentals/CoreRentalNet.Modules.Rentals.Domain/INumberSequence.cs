namespace CoreRentalNet.Modules.Rentals.Domain;

/// <summary>
/// Hands out order and invoice numbers.
/// </summary>
/// <remarks>
/// Reserving happens immediately rather than inside the unit of work, so two concurrent
/// checkouts cannot take the same number. A reservation that is never used leaves a gap in the
/// sequence, which is normal and harmless: numbers only have to be unique and increasing.
/// </remarks>
public interface INumberSequence
{
    Task<int> ReserveNextAsync(SequenceKind kind, int year, CancellationToken cancellationToken = default);
}
