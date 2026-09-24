namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// A phrase that is measured rather than asserted, with the vector it was measured with.
/// </summary>
/// <remarks>
/// These are the price words — <c>luxury</c>, <c>exclusive</c> — that the model does not map to price. They are
/// recorded so the gap is visible and so a change of model fails the characterisation rather than passing
/// silently.
/// </remarks>
internal sealed record CatalogEmbeddingFixturePhrase(string Phrase, byte[] Vector);
