using System.Text.Json.Serialization;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>What the browser posts to start a run: the customer's sentence, and a ceiling if they gave one.</summary>
/// <remarks>
/// <para>
/// Deliberately the whole of the request. The browser does not send a draft, a slot, a SKU or a price: the
/// catalogue and the slot rules are the application's, and a caller that could name them could name a
/// product the catalogue does not hold.
/// </para>
/// <para>
/// <c>Query</c> is nullable because the framework will not reject a body that omits it, and the endpoint
/// checks it rather than trusting the caller. A run costs roughly 172k input tokens, so an empty sentence
/// is refused before anything is spent rather than sent and refused by the model.
/// </para>
/// </remarks>
internal sealed record RunRequest(
    [property: JsonPropertyName("query")] string? Query,
    [property: JsonPropertyName("ceilingMonthly")] int? CeilingMonthly);
