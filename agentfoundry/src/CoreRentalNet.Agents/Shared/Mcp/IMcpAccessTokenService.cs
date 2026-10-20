namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>Compatibility seam for tests written before the token moved to the run context.</summary>
internal interface IMcpAccessTokenService : IDisposable
{
    string? Token { get; set; }

    ValueTask<string> GetAsync(CancellationToken cancellationToken);

    void Release();
}
