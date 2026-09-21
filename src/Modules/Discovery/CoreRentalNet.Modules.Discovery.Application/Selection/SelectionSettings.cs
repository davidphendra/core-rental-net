namespace CoreRentalNet.Modules.Discovery.Application.Selection;

/// <summary>How quickly a selection is forgotten, and by how much it may move a product.</summary>
/// <remarks>
/// <para>
/// <b>Decay exists because the signal is query-independent.</b> Offered and chosen counts say nothing about what
/// a customer asked for, so without decay every product chosen often becomes a favourite for every request: a
/// lamp picked once for a reading corner would be boosted for a coffee bar. A half-life bounds how long a
/// selection can speak for, and it is a tuning knob for the same reason the spread factor and the shortlist size
/// are — whether a month is the right memory is a measurement.
/// </para>
/// <para>
/// <b>Both counters decay at the same rate, and that is what keeps the score readable.</b> The score is a ratio,
/// so uniform decay cancels out of it: the read path needs no clock at all, and retrieval stays a pure function
/// of the rows.
/// </para>
/// </remarks>
public sealed record SelectionSettings(TimeSpan HalfLife)
{
    /// <summary>What a deployment that has tuned nothing gets.</summary>
    public static readonly TimeSpan DefaultHalfLife = TimeSpan.FromDays(30);

    /// <summary>The default.</summary>
    public static SelectionSettings Default { get; } = new(DefaultHalfLife);
}
