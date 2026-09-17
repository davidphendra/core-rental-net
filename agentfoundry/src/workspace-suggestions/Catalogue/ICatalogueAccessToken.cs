namespace AgentFoundry.WorkspaceSuggestions.Catalogue;

/// <summary>The access token the catalogue endpoint is read with.</summary>
/// <remarks>
/// <para>
/// A port rather than a credential, and that is what keeps the catalogue reachable before the grant has
/// been settled: whether the token is minted by the platform's toolbox or by the container's own
/// identity is a decision for the deployment, and either way this code is handed a string. The reader
/// never learns where it came from, which is the only way the two answers can be swapped without
/// touching the agent.
/// </para>
/// <para>
/// Asked for per read rather than once, because an access token expires: a value captured when the
/// process started would be the reason a long-lived container stopped being able to see the catalogue,
/// and the failure would arrive hours after the cause.
/// </para>
/// </remarks>
public interface ICatalogueAccessToken
{
    ValueTask<string> TokenAsync(CancellationToken cancellationToken = default);
}
