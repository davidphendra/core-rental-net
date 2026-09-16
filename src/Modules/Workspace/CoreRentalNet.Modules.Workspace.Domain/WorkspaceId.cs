using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

/// <summary>A draft's identity, and the value a checkout reports back to the customer.</summary>
public readonly record struct WorkspaceId(Guid Value)
{
    /// <summary>A new, non-empty identity.</summary>
    public static WorkspaceId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing identity.</summary>
    /// <exception cref="DomainRuleViolationException">The value is <see cref="Guid.Empty"/>.</exception>
    public static WorkspaceId From(Guid value)
        => value == Guid.Empty
            ? throw new DomainRuleViolationException("A workspace id cannot be empty.")
            : new WorkspaceId(value);

    public override string ToString() => Value.ToString();
}
