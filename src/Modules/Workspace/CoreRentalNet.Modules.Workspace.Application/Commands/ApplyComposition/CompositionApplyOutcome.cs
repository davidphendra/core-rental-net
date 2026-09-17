namespace CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;

/// <summary>What became of an attempt to replace a workspace's slots.</summary>
/// <remarks>
/// Two outcomes rather than a boolean, because a rejected write is not a failure of the operation: the
/// workspace is exactly as it was, and what the caller needs to know is that somebody else changed it.
/// The names are the words the page says, so nothing downstream has to interpret a flag.
/// </remarks>
public enum CompositionApplyOutcome
{
    /// <summary>The composition is the workspace's contents now.</summary>
    Applied,

    /// <summary>The draft moved on since the composition was chosen, so nothing was written.</summary>
    Stale,
}
