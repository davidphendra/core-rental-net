namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>The one place a run's ending is stated: what is closed, and in what order.</summary>
internal interface IRunEnding
{
    /// <summary>Closes the catalogue session a run opened, then gives up the caller's token.</summary>
    ValueTask EndTheRunAsync();
}
