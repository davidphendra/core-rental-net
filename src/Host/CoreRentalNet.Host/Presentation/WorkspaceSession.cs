using CoreRentalNet.BuildingBlocks.Application;
using CoreRentalNet.BuildingBlocks.Domain;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.AssignProduct;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.ChangeQuantity;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.RemoveAssignment;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.SetDeliveryAddress;
using CoreRentalNet.Modules.Workspace.Application.Workspace.Commands.StartDraft;
using CoreRentalNet.Modules.Workspace.Application.Queries.GetWorkspace;
using CoreRentalNet.Modules.Workspace.Application.Queries.Views;
using CoreRentalNet.Modules.Workspace.Domain;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// View state for the current circuit.
/// </summary>
/// <remarks>
/// Deliberately a cache, never a source of truth: every value here came from the database via
/// a handler, and every read refreshes it. A command handler mutates and returns nothing, so a
/// successful mutation is followed by one read through <see cref="IGetWorkspaceHandler"/> — the
/// write path never builds the view. A refusal from the domain is surfaced as a message rather
/// than an exception, so the customer sees why nothing happened instead of the page breaking.
/// </remarks>
public sealed class WorkspaceSession(
    IStartDraftHandler starter,
    IGetWorkspaceHandler reader,
    IAssignProductHandler assigner,
    IRemoveAssignmentHandler remover,
    IChangeQuantityHandler quantityChanger,
    ISetDeliveryAddressHandler addressSetter) : IWorkspaceSession
{
    /// <inheritdoc />
    public WorkspaceView? Current { get; private set; }

    /// <inheritdoc />
    public string? Error { get; private set; }

    /// <inheritdoc />
    public bool IsLoaded => Current is not null;

    /// <inheritdoc />
    public bool IsEmpty => Current is null || Current.IsEmpty;

    /// <inheritdoc />
    public int TotalUnits => Current?.TotalUnits ?? 0;

    /// <inheritdoc />
    public event Action? Changed;

    /// <inheritdoc />
    public async Task RefreshAsync(string draftToken, CancellationToken cancellationToken = default)
    {
        Current = await reader.HandleAsync(new GetWorkspaceQuery(draftToken), cancellationToken);
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

        await starter.HandleAsync(new StartDraftCommand(draftToken), cancellationToken);
        Current = await reader.HandleAsync(new GetWorkspaceQuery(draftToken), cancellationToken);
        Error = null;
        Notify();
    }

    /// <inheritdoc />
    public Task AssignAsync(string draftToken, string sku, CancellationToken cancellationToken = default)
        => MutateAsync(draftToken, () => assigner.HandleAsync(new AssignProductCommand(draftToken, sku), cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task RemoveAsync(string draftToken, SlotId slot, CancellationToken cancellationToken = default)
        => MutateAsync(draftToken, () => remover.HandleAsync(new RemoveAssignmentCommand(draftToken, slot), cancellationToken), cancellationToken);

    /// <summary>
    /// Sets how many of one product a slot holds. Zero removes it, which is how a single box on the
    /// canvas is taken away when the same product stands in more than one.
    /// </summary>
    public Task SetQuantityAsync(string draftToken, SlotId slot, string sku, int quantity, CancellationToken cancellationToken = default)
        => MutateAsync(draftToken, () => quantityChanger.HandleAsync(new ChangeQuantityCommand(draftToken, slot, sku, quantity), cancellationToken), cancellationToken);

    /// <inheritdoc />
    public Task SetDeliveryAddressAsync(string draftToken, string? address, CancellationToken cancellationToken = default)
        => MutateAsync(draftToken, () => addressSetter.HandleAsync(new SetDeliveryAddressCommand(draftToken, address), cancellationToken), cancellationToken);

    /// <summary>
    /// Saves the address and hands any refusal back to the caller instead of leaving it on the
    /// session, where the next page would show it as though it belonged to something else.
    /// </summary>
    public async Task<string?> TrySetDeliveryAddressAsync(string draftToken, string? address, CancellationToken cancellationToken = default)
    {
        Error = null;

        return await AttemptAsync(draftToken, () => addressSetter.HandleAsync(new SetDeliveryAddressCommand(draftToken, address), cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ClearError() => Error = null;

    /// <summary>
    /// Runs one command and keeps the session's refusal rule in one place: an expected domain
    /// refusal is a message, not an exception. Returns null when the command was accepted, and the
    /// message when it was refused. On success it re-reads the cache from the database, because the
    /// cache is never the source of truth and the command returned no view.
    /// </summary>
    private async Task<string?> AttemptAsync(string draftToken, Func<Task> operation, CancellationToken cancellationToken)
    {
        try
        {
            await operation().ConfigureAwait(false);
            Current = await reader.HandleAsync(new GetWorkspaceQuery(draftToken), cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (Exception exception) when (exception is DomainRuleViolationException or NotFoundException)
        {
            return exception.Message;
        }
    }

    /// <summary>A mutation the customer is expected to see the result of: a refusal becomes the session's error.</summary>
    private async Task MutateAsync(string draftToken, Func<Task> operation, CancellationToken cancellationToken)
    {
        Error = await AttemptAsync(draftToken, operation, cancellationToken).ConfigureAwait(false);
        Notify();
    }

    private void Notify() => Changed?.Invoke();
}
