namespace CoreRentalNet.Host.Presentation;

/// <summary>What the identity SDK says about this session's ability to call the catalogue.</summary>
/// <remarks>
/// <para>
/// <b>Both members are the SDK's answers, not this application's.</b> <see cref="CanRunSuggestion"/> is whether
/// the SDK could produce a valid access token - a still-valid one, or a refreshed one - and it is false when it
/// could not. <see cref="SessionExpiresAt"/> is the upstream ceiling the SDK enforces, or null when the
/// connection emits no such claim.
/// </para>
/// <para>
/// <b>No token and no expiry are computed here.</b> The SDK does not expose the access token's own expiry, and
/// reading it from the ticket would be exactly the hand-rolled expiry logic this endpoint exists to avoid.
/// </para>
/// </remarks>
public sealed record SessionStatus(bool CanRunSuggestion, DateTimeOffset? SessionExpiresAt);
