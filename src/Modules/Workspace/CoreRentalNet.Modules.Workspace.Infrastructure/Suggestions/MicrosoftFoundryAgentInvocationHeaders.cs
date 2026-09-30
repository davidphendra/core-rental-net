namespace CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;

/// <summary>The headers the application states a run's out-of-band facts on.</summary>
/// <remarks>
/// <para>
/// <b>A header rather than a field of the request payload, because the payload is a message.</b> The body is what
/// the hosting layer records and what a model may be handed, and a bearer token belongs in neither.
/// </para>
/// <para>
/// <b>The <c>x-client-</c> prefix is the platform's forwarding rule, so it is part of the name rather than
/// decoration.</b> The hosting layer's request context exposes the client headers — those prefixed with
/// <c>x-client-</c> — so a name outside that prefix is not forwarded at all, and the failure is silent: the run
/// would simply find no token at the far end.
/// </para>
/// </remarks>
public static class MicrosoftFoundryAgentInvocationHeaders
{
    /// <summary>The caller's own catalogue token, for the one run being started.</summary>
    public const string CallerAccessToken = "x-client-caller-access-token";
}
