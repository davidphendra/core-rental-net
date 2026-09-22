namespace CoreRentalNet.Host.Enums;

/// <summary>
/// Which of a product's fields a caller asks for.
/// </summary>
/// <remarks>
/// A projection of the module's published view, not a second vocabulary: it is the same product with
/// the display fields a machine caller cannot use left out, so <c>ProductView</c> stays the only
/// description of a product. Absent means <see cref="Full"/>, so a caller that says nothing sees what
/// it saw before, and a word that is not one of these is refused with the words that would have
/// worked - the same answer an unknown filter gets.
/// </remarks>
public enum CatalogProjection
{
    /// <summary>Every field the module publishes. What a caller gets when it asks for nothing.</summary>
    Full = 1,

    /// <summary>The fields a machine caller reads: no image path, no display flags.</summary>
    Compact = 2,
}
