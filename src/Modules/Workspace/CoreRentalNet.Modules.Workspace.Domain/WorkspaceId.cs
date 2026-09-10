using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Workspace.Domain;

public readonly record struct WorkspaceId(Guid Value)
{
    public static WorkspaceId New() => new(Guid.NewGuid());

    public static WorkspaceId From(Guid value)
        => value == Guid.Empty
            ? throw new DomainRuleViolationException("A workspace id cannot be empty.")
            : new WorkspaceId(value);

    public override string ToString() => Value.ToString();
}
