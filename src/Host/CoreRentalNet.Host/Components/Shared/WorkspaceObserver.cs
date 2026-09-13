using CoreRentalNet.Host.Presentation;
using Microsoft.AspNetCore.Components;

namespace CoreRentalNet.Host.Components.Shared;

/// <summary>
/// A component that redraws when the workspace changes.
/// </summary>
/// <remarks>
/// The draft is shared state, and the panel that changes it is no longer the component that shows
/// the result: assigning a chair happens in the docked panel while the canvas sits in the page
/// beside it. Without this, the assignment lands in the database and the screen keeps showing the
/// old workspace until something else happens to redraw it.
/// </remarks>
public abstract class WorkspaceObserver : ComponentBase, IDisposable
{
    [Inject]
    protected IWorkspaceSession Session { get; set; } = default!;

    protected override void OnInitialized() => Session.Changed += OnWorkspaceChanged;

    private void OnWorkspaceChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Session.Changed -= OnWorkspaceChanged;
        GC.SuppressFinalize(this);
    }
}
