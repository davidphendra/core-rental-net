using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Commands;
using CoreRentalNet.Modules.Workspace.Application.Queries;
using CoreRentalNet.Modules.Workspace.Application.Workspace;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// View state for the current circuit.
/// </summary>
/// <remarks>
/// Deliberately a cache, never a source of truth: every value here came from the database via
/// a handler, and every read refreshes it (ADR-0006, decision 10.5). A refusal from the domain
/// is surfaced as a message rather than an exception, so the customer sees why nothing
/// happened instead of the page breaking.
/// </remarks>
public sealed class WorkspaceSession(
    StartDraftHandler starter,
    GetWorkspaceHandler reader,
    AssignProductHandler assigner,
    RemoveAssignmentHandler remover,
    ChangeQuantityHandler quantityChanger,
    SetDeliveryAddressHandler addressSetter)
{
    public WorkspaceView? Current { get; private set; }

    public string? Error { get; private set; }

    public bool IsLoaded => Current is not null;

    public bool IsEmpty => Current is null || Current.IsEmpty;

    public int TotalUnits => Current?.TotalUnits ?? 0;

    public event Action? Changed;

    public async Task RefreshAsync(string draftToken, CancellationToken cancellationToken = default)
    {
        Current = await reader.HandleAsync(new GetWorkspace(draftToken), cancellationToken);
        Error = null;
        Notify();
    }

    /// <summary>
    /// Makes sure this browser has a draft and loads it, once per circuit. A first-time visitor
    /// has a cookie but no draft yet, so something has to create it.
    /// </summary>
    public async Task EnsureLoadedAsync(string draftToken, CancellationToken cancellationToken = default)
    {
        if (Current is not null)
        {
            return;
        }

        Current = await starter.HandleAsync(new StartDraft(draftToken), cancellationToken);
        Error = null;
        Notify();
    }

    public Task AssignAsync(string draftToken, string sku, CancellationToken cancellationToken = default)
        => MutateAsync(() => assigner.HandleAsync(new AssignProduct(draftToken, sku), cancellationToken));

    public Task RemoveAsync(string draftToken, SlotId slot, CancellationToken cancellationToken = default)
        => MutateAsync(() => remover.HandleAsync(new RemoveAssignment(draftToken, slot), cancellationToken));

    /// <summary>
    /// Sets how many of one product a slot holds. Zero removes it, which is how a single box on the
    /// canvas is taken away when the same product stands in more than one.
    /// </summary>
    public Task SetQuantityAsync(string draftToken, SlotId slot, string sku, int quantity, CancellationToken cancellationToken = default)
        => MutateAsync(() => quantityChanger.HandleAsync(new ChangeQuantity(draftToken, slot, sku, quantity), cancellationToken));

    public Task SetDeliveryAddressAsync(string draftToken, string? address, CancellationToken cancellationToken = default)
        => MutateAsync(() => addressSetter.HandleAsync(new SetDeliveryAddress(draftToken, address), cancellationToken));

    /// <summary>
    /// Saves the address and hands any refusal back to the caller instead of leaving it on the
    /// session, where the next page would show it as though it belonged to something else.
    /// </summary>
    public async Task<string?> TrySetDeliveryAddressAsync(string draftToken, string? address, CancellationToken cancellationToken = default)
    {
        Error = null;

        try
        {
            Current = await addressSetter.HandleAsync(new SetDeliveryAddress(draftToken, address), cancellationToken);
            return null;
        }
        catch (DomainRuleViolationException exception)
        {
            Error = null;
            return exception.Message;
        }
        catch (NotFoundException exception)
        {
            Error = null;
            return exception.Message;
        }
    }

    public void ClearError() => Error = null;

    private async Task MutateAsync(Func<Task<WorkspaceView>> operation)
    {
        Error = null;

        try
        {
            Current = await operation();
        }
        catch (DomainRuleViolationException exception)
        {
            Error = exception.Message;
        }
        catch (NotFoundException exception)
        {
            Error = exception.Message;
        }

        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
