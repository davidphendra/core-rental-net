namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// How large a shortlist is built.
/// </summary>
/// <remarks>
/// <para>
/// It is a record rather than a constant because it is the first thing the evaluation tier will want to move.
/// Two per bucket is the number this epic chose, and it was chosen knowing what it costs: a shortlist that
/// takes only the nearest products cannot also guarantee a cheap and a dear option in every bucket, and
/// <c>SuggestionValidator.Spread</c> collapses a run to a single candidate when the dearest is under 1.5x the
/// cheapest. The collapse rate is a recorded baseline, and this number is the knob that moves it.
/// </para>
/// <para>
/// The composition root reads it, the same way the Host reads the spread factor: the module has no
/// configuration dependency, so whoever composes decides where the number comes from.
/// </para>
/// </remarks>
public sealed record ShortlistSettings(int PerBucket, double BoostWeight)
{
    /// <summary>What a deployment that has tuned nothing gets.</summary>
    public const int DefaultPerBucket = 2;

    /// <summary>
    /// How much a customer's past choices may move a product within its bucket. Zero turns the signal off.
    /// </summary>
    /// <remarks>
    /// It is the second knob the evaluation tier will want, and it belongs beside the first: both decide what a
    /// shortlist looks like. It is small on purpose — the signal only ever reorders products that similarity
    /// already chose, so a large weight would let a well-liked product sit above a far better match.
    /// </remarks>
    public const double DefaultBoostWeight = 0.1;

    /// <summary>The default.</summary>
    public static ShortlistSettings Default { get; } = new(DefaultPerBucket, DefaultBoostWeight);
}
