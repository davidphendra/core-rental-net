namespace CoreRentalNet.Host.AiBuilder;

/// <summary>What one run was, in a shape that can be answered after the fact.</summary>
/// <remarks>
/// <para>
/// A paid, quality-sensitive call has to be explainable later: which prompt, which model, how many calls, how
/// much it cost, and what the customer asked. This is that, and nothing more.
/// </para>
/// <para>
/// <b>No PII.</b> The customer is an <b>opaque id</b> - a hash of the account this application already
/// identifies them by - and there is no name, no email and no address anywhere on it. The query is here
/// because a run is unexplainable without it, and it is the customer's own words rather than anything derived
/// from their account.
/// </para>
/// <para>
/// <b>The raw output is kept, and it is the point.</b> A rejected run is the one worth debugging, and the
/// model's own answer is the only evidence that can tell a bad prompt from a bad catalogue. Under the
/// purpose-only rule it carries no price and no product name, so keeping it costs nothing in privacy. It is
/// also what the evaluation tier grades - the model's output, rather than what the application salvaged.
/// </para>
/// <para>
/// <b>The payload hash is of the projection actually sent</b>, not of the catalogue file: a change to the
/// projection is a change to what the model saw, and hashing the file would miss it.
/// </para>
/// <para>
/// <b>What the run was GIVEN is here, not only what it produced.</b> An answer drawn from fourteen products
/// cannot be explained without knowing which fourteen, and a poor one is otherwise indistinguishable from a
/// model that chose badly among good options. The embedding model and width travel with it for the same reason:
/// a shortlist is only explicable against the deployment whose vectors put it in that order, and the width is
/// what the query vector had to match.
/// </para>
/// <para>
/// The applied candidate is NOT here, and that is not an omission: it is not known when a run ends. It is
/// written as <see cref="AiRunApplication"/> when a customer chooses one, and the two are joined on
/// <see cref="RunId"/>.
/// </para>
/// </remarks>
internal sealed record AiRunRecord(
    string RunId,
    string Query,
    string PayloadHash,
    IReadOnlyList<string> Shortlist,
    string EmbeddingModel,
    int EmbeddingWidth,
    string Model,
    string PromptVersion,
    int ModelCalls,
    int InputTokens,
    int OutputTokens,
    long LatencyMilliseconds,
    string RawOutput,
    AiRunVerdict Verdict,
    string CustomerId);
