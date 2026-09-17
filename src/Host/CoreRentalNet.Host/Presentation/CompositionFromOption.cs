using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Commands.ApplyComposition;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

namespace CoreRentalNet.Host.Presentation;

/// <summary>
/// Turns the candidate a customer chose into something the workspace can be replaced with.
/// </summary>
/// <remarks>
/// <para>
/// The candidate is the agent's answer and the composition is the workspace's instruction, and this is
/// where one becomes the other. What does not cross is everything the agent said about <em>why</em>: the
/// criteria, the tier's name and the findings are the page's business, and the Workspace module has no
/// use for them.
/// </para>
/// <para>
/// The catalogue is checked again here rather than trusted from the run, because the two are minutes
/// apart and a product can leave the catalogue in between. A line the catalogue no longer holds is
/// dropped and named, so a partial composition is never applied in silence - and dropping it here, before
/// the write, is what keeps the workspace from holding a product it cannot quote.
/// </para>
/// </remarks>
internal static class CompositionFromOption
{
    public static (IReadOnlyList<CompositionLine> Lines, IReadOnlyList<string> Dropped) From(
        SuggestedOption option,
        IProductCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(option);
        ArgumentNullException.ThrowIfNull(catalog);

        var lines = new List<CompositionLine>(option.Lines.Count);
        var dropped = new List<string>();

        foreach (var line in option.Lines)
        {
            if (catalog.Find(line.Sku) is null)
            {
                dropped.Add(line.Sku);
                continue;
            }

            lines.Add(new CompositionLine(line.Slot, line.Sku, line.Quantity));
        }

        return (lines, dropped);
    }
}
