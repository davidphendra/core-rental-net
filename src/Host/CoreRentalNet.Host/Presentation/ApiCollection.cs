namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// A collection answer: the items, and how many there are.
/// </summary>
/// <remarks>
/// An object rather than a bare array, because a list response is the one that most often has to grow -
/// a count today, pages later - and an array as the whole body cannot gain a sibling field without
/// breaking every caller that reads it. The field is named <c>value</c> because that is the name the
/// Microsoft REST API Guidelines give it, so a generated client and a hand-written one read the same
/// thing. Paging is deliberately absent: the catalogue is 62 items, and the envelope is what makes it
/// addable later without a break.
/// </remarks>
/// <param name="Value">The items, in the order the module published them.</param>
/// <param name="Count">How many items this answer carries.</param>
public sealed record ApiCollection<T>(IReadOnlyList<T> Value, int Count);
