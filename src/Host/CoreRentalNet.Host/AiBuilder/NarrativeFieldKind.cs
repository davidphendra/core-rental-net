namespace CoreRentalNet.Host.AiBuilder;

/// <summary>Which piece of the model's own words a streamed fragment is.</summary>
/// <remarks>
/// Exactly the two free-text fields the contract permits to reach a customer. Everything else in the
/// answer — the SKU, the quantity, the slot, the status — is structure, and structure is never streamed:
/// it is validated first and shown after.
/// </remarks>
internal enum NarrativeFieldKind
{
    /// <summary>An option's rationale: why this setup, in the model's words.</summary>
    Rationale,

    /// <summary>One line's <c>why</c>: what this item is for.</summary>
    Why,
}
