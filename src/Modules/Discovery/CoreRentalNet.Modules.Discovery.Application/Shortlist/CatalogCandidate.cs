namespace CoreRentalNet.Modules.Discovery.Application.Shortlist;

/// <summary>
/// A product as the ranking sees it: its key, its bucket, and its vector.
/// </summary>
/// <remarks>
/// It exists so the ranking can be a <b>pure function over data</b> rather than something that only runs
/// against a database. Ranking is where the shortlist's whole shape is decided — two per bucket, fourteen in
/// all, one order and no other — and a rule that can only be exercised through SQLite is a rule that gets
/// tested once, coarsely, in the tier that has a file. Handing the ranking its candidates makes every one of
/// those properties a unit test.
/// </remarks>
public sealed record CatalogCandidate(string Sku, CatalogBucket Bucket, float[] Embedding);
