namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// Which deployment would embed a request, and how wide its vectors are.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists so that the run record can name the retrieval without the run knowing about configuration.</b>
/// The record has to say which model embedded the query and at what width, because a shortlist is only
/// explicable against the deployment that produced the vectors behind it — and a run whose answers look wrong
/// for no visible reason is exactly the run somebody has to explain later.
/// </para>
/// <para>
/// The composition root builds it from the same settings the embedding client was built from, so the two cannot
/// disagree about which deployment a run actually used.
/// </para>
/// </remarks>
internal sealed record RetrievalFacts(string EmbeddingModel, int EmbeddingWidth);
