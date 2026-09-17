using System.Runtime.CompilerServices;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;

namespace CoreRentalNet.Modules.Workspace.Application.Services;

/// <summary>
/// The rules that stand between what the agent said and what a customer is shown.
/// </summary>
/// <remarks>
/// <para>
/// The agent is not trusted with the catalogue it read, and it should not be: it is a language model
/// answering over an HTTP call, and the two failures that matter are that it names a product that does
/// not exist and that it states an amount. So every SKU is resolved here and every price is the
/// catalogue's, and a SKU that resolves to nothing is dropped and reported rather than passed on.
/// </para>
/// <para>
/// Nothing is padded. Fewer valid candidates than the agent claimed is the honest answer, and an option
/// whose lines all resolve to nothing is not shown at all: a candidate with no products in it is not a
/// cheaper candidate, it is an empty one.
/// </para>
/// <para>
/// Stages pass through as they arrive, before any of this applies: they say where the run is, and a page
/// that waited for the checking before showing them would have nothing to show while a customer waits.
/// </para>
/// </remarks>
public sealed class SuggestWorkspaceOptions(IAgentSuggestions agent, IProductCatalog catalog)
    : ISuggestWorkspaceOptions
{
    public async IAsyncEnumerable<SuggestionUpdate> SuggestAsync(
        WorkspaceSuggestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!agent.IsConfigured)
        {
            yield return SuggestionUpdate.Answer(Unavailable());

            yield break;
        }

        await foreach (var message in MessagesAsync(request, cancellationToken))
        {
            if (message.Result is { } answer)
            {
                yield return SuggestionUpdate.Answer(Checked(answer));

                yield break;
            }

            yield return SuggestionUpdate.StageEvent(message.Stage ?? string.Empty, message.Attempt);
        }

        // The agent's stream ended without an answer. That is a transport failure, not a refusal.
        yield return SuggestionUpdate.Answer(Unavailable());
    }

    /// <summary>
    /// What the agent sent, or nothing when it could not be reached.
    /// </summary>
    /// <remarks>
    /// A failure to reach the agent is not a refusal: the page says something different for it, and only
    /// one of the two invites a retry. The exception is caught around the enumeration rather than inside
    /// it, because a stream fails while it is being read and not when it is asked for.
    /// </remarks>
    private async IAsyncEnumerable<AgentSuggestionMessage> MessagesAsync(
        WorkspaceSuggestionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = agent
            .AskAsync(request.Query, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            var (moved, current) = await NextAsync(stream, cancellationToken);

            if (!moved)
            {
                yield break;
            }

            yield return current!;
        }
    }

    /// <summary>The next message, or nothing when the stream ended or could not be read.</summary>
    /// <remarks>
    /// One step of the enumeration, in its own method so that the failure is caught at one nesting level
    /// rather than four. A stream fails while it is read, which is why this is not a try around the call
    /// that created it.
    /// </remarks>
    /// <summary>
    /// The next message, or nothing when the agent could not be reached.
    /// </summary>
    /// <remarks>
    /// A timeout and a caller who cancelled both arrive as <see cref="TaskCanceledException"/>, and they
    /// mean opposite things: the first is the agent being unavailable, and the second is a customer who
    /// asked us to stop. Swallowing the second would let a cancelled run report itself as an unavailable
    /// service, so it is passed on and only an unreachable agent becomes that answer.
    /// </remarks>
    private static async Task<(bool Moved, AgentSuggestionMessage? Current)> NextAsync(
        IAsyncEnumerator<AgentSuggestionMessage> stream,
        CancellationToken cancellationToken)
    {
        try
        {
            return (await stream.MoveNextAsync(), stream.Current);
        }
        catch (Exception failure) when (failure is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            return (false, null);
        }
    }

    /// <summary>The candidates the catalogue can supply, with the SKUs it cannot supply removed.</summary>
    private WorkspaceSuggestion Checked(AgentSuggestion answer)
    {
        // A refusal carries nothing: no options and no findings, because there was nothing to object to.
        if (answer.Status == SuggestionStatus.Rejected)
        {
            return new WorkspaceSuggestion(SuggestionStatus.Rejected, [], answer.Code, [], []);
        }

        var dropped = new List<string>();
        var options = new List<SuggestedOption>(answer.Options.Count);

        foreach (var option in answer.Options)
        {
            var lines = Lines(option, dropped);

            if (lines.Count > 0)
            {
                options.Add(new SuggestedOption(
                    option.Tier,
                    lines,
                    option.Criteria,
                    option.Unevaluated,
                    option.PinnedSlots));
            }
        }

        return new WorkspaceSuggestion(
            answer.Status,
            options,
            answer.Code,
            dropped,
            [.. answer.Findings.Select(finding => new SuggestionFinding(finding.Kind, finding.Slot))]);
    }

    /// <summary>The lines whose SKUs the catalogue holds, in the order the agent gave them.</summary>
    private IReadOnlyList<SuggestedLine> Lines(AgentSuggestionOption option, List<string> dropped)
    {
        var lines = new List<SuggestedLine>(option.Lines.Count);

        foreach (var line in option.Lines)
        {
            var product = catalog.Find(line.Sku);

            if (product is null)
            {
                dropped.Add(line.Sku);

                continue;
            }

            lines.Add(new SuggestedLine(line.Slot, product.Sku, product.Name, line.Quantity, product.MonthlyPrice));
        }

        return lines;
    }

    /// <summary>
    /// The application's own state, and never the agent's.
    /// </summary>
    /// <remarks>
    /// The agent's contract has three statuses and all of them are answers, so <c>unavailable</c> exists
    /// only here: it means no agent is configured, or the one that is could not be reached. That is why
    /// it is the only outcome offering a retry - a refusal and an exhaustion are answers, and asking
    /// again with the same words would produce the same one.
    /// </remarks>
    private static WorkspaceSuggestion Unavailable()
        => new(SuggestionStatus.Unavailable, [], Code: null, DroppedSkus: [], Findings: []);
}
