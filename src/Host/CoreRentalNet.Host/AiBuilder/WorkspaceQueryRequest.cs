using System.ComponentModel.DataAnnotations;
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
/// <c>Query</c> is required, and the requirement is declared here rather than checked in the endpoint: a run
/// needs a sentence, and <c>[ApiController]</c> refuses a body that does not carry one before the action runs -
/// which is where every other refusal in this application's API is made. A run costs roughly 172k input tokens,
/// so an empty sentence is refused before anything is spent rather than sent and refused by the model.
/// </para>
/// <para>
/// <b>The attribute is on the constructor parameter, and that is not interchangeable with the property.</b> A
/// record exposes both, and MVC refuses to read validation metadata from the property: written as
/// <c>[property: Required]</c> it throws <c>InvalidOperationException</c> on the first request that binds a body,
/// because the metadata "will be ignored". It compiles either way, which is why this is written down.
/// </para>
/// </remarks>
public sealed record WorkspaceQueryRequest(
    [property: JsonPropertyName("query")]
    [param: Required] string? Query,
    [property: JsonPropertyName("ceilingMonthly")] int? CeilingMonthly);
