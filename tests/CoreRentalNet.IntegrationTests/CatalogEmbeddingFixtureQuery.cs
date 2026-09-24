namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// One paraphrase in the fixture: the phrase, the filter it is asked with, the product it must find, and the
/// phrase's own vector.
/// </summary>
/// <remarks>
/// The phrase is stored so the fixture can be read as a statement of what a customer might type; the vector is
/// what the search actually uses, which is why the test can stay offline and still exercise the real model's
/// geometry. <c>Category</c> and <c>SubCategory</c> are the narrowing the agent does; both null means the
/// phrase was asked across the whole catalogue.
/// </remarks>
internal sealed record CatalogEmbeddingFixtureQuery(
    string Kind,
    string Phrase,
    string? Category,
    string? SubCategory,
    string ExpectedSku,
    byte[] Vector);
