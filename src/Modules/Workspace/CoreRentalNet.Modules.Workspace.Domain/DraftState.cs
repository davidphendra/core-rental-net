namespace CoreRentalNet.Modules.Workspace.Domain;

public enum DraftState
{
    /// <summary>Still being composed by the customer.</summary>
    Draft = 1,

    /// <summary>Turned into an order. This is what makes checkout idempotent (ADR-0010).</summary>
    Converted = 2,
}
