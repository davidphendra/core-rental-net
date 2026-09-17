namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// A collection answer: the items, how many there are, and how many matched.
/// </summary>
/// <remarks>
/// <para>
/// An object rather than a bare array, because a list response is the one that most often has to grow -
/// a count today, pages later - and an array as the whole body cannot gain a sibling field without
/// breaking every caller that reads it. The field is named <c>value</c> because that is the name the
/// Microsoft REST API Guidelines give it, so a generated client and a hand-written one read the same
/// thing.
/// </para>
/// <para>
/// <c>total</c> is how many products matched the filters and <c>count</c> is how many this answer
/// carries. They are the same number until the answer is capped, and a caller that cannot tell them
/// apart cannot tell a complete answer from a partial one - which matters here because what a caller
/// does with the rows depends on knowing it has all of them. <c>truncated</c> is derived from the two
/// rather than passed in, so it cannot disagree with them.
/// </para>
/// </remarks>
/// <param name="Value">The items, in the order the module published them.</param>
/// <param name="Count">How many items this answer carries.</param>
/// <param name="Total">How many items matched the filters.</param>
public sealed record ApiCollection<T>(IReadOnlyList<T> Value, int Count, int Total)
{
    /// <summary>True when this answer carries fewer items than matched.</summary>
    public bool Truncated => Count < Total;
}
