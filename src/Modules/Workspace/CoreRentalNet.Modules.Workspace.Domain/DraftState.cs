namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>Where a draft is in its life: still composed, or already converted.</summary>
public enum DraftState
{
    /// <summary>Still being composed by the customer.</summary>
    Draft = 1,

    /// <summary>Turned into an order. This is what makes checkout idempotent.</summary>
    Converted = 2,
}
