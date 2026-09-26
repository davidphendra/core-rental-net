using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;

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
/// model's own answer is the only evidence that can tell a bad prompt from a bad catalogue.
/// </para>
/// </remarks>
public sealed record WorkspaceSuggestionRunRecord(
    string RunId,
    string Query,
    string PayloadHash,
    string Model,
    string PromptVersion,
    int ModelCalls,
    int InputTokens,
    int OutputTokens,
    long LatencyMilliseconds,
    string RawOutput,
    WorkspaceSuggestionVerdict Verdict,
    string CustomerId);
