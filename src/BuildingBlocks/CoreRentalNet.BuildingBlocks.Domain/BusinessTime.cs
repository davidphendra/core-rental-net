namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>
/// The business calendar. Core Rental operates in Bali, so a date is the date it is there, not
/// the date it is in UTC.
/// </summary>
/// <remarks>
/// Indonesia observes no daylight saving, so this is a fixed offset and needs no timezone
/// database. An order placed at 01:00 on 1 February in Denpasar anchors to 1 February rather
/// than to 31 January (ADR-0011).
/// </remarks>
public static class BusinessTime
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(8);

    public static DateOnly BusinessDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToOffset(UtcOffset).DateTime);

    public static DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        return BusinessDate(timeProvider.GetUtcNow());
    }

    /// <summary>A month index counted from an anchor, using the calendar rather than 30 days.</summary>
    public static DateOnly AddMonths(DateOnly anchor, int months) => anchor.AddMonths(months);
}
