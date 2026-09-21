using CoreRentalNet.Modules.Discovery.Application.Selection;
using CoreRentalNet.Modules.Workspace.Application.Commands.ReplaceComposition;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>
/// Credits the products of a composition the customer actually applied.
/// </summary>
/// <remarks>
/// <para>
/// <b>A decorator rather than a line in the page.</b> The apply happens in the builder component, which reaches
/// the application through this interface; crediting there would put a Discovery concern in the markup and make
/// the rule provable only through a browser. Wrapping the handler keeps the signal where the command is, and
/// keeps <c>Workspace</c> entirely unaware that a selection signal exists.
/// </para>
/// <para>
/// <b>Only after the write succeeded.</b> A refused apply — a stale version, a composition that does not fit —
/// applied nothing, so crediting it would record a choice the customer never got.
/// </para>
/// <para>
/// <b>A credit that cannot be recorded never fails the apply.</b> The composition is already written; losing one
/// credit is a fact to log, not a reason to tell a customer their saved workspace did not save.
/// </para>
/// </remarks>
internal sealed class CreditingReplaceComposition(
    IReplaceCompositionHandler inner,
    ICreditSelection credit,
    ILoggerFactory loggers) : IReplaceCompositionHandler
{
    private const string Category = "CoreRentalNet.Host.AiBuilder.SelectionSignal";

    /// <inheritdoc />
    public async Task HandleAsync(ReplaceCompositionCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await inner.HandleAsync(command, cancellationToken).ConfigureAwait(false);

        try
        {
            await credit
                .CreditAsync([.. command.Lines.Select(line => line.Sku)], cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            loggers.CreateLogger(Category).LogError(
                exception,
                "The applied composition could not be credited, so it will not count towards any score.");
        }
    }
}
