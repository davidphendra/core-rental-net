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
/// </remarks>
public sealed class SuggestWorkspaceOptions(IAgentSuggestions agent, IProductCatalog catalog)
    : ISuggestWorkspaceOptions
{
    public async Task<WorkspaceSuggestion> SuggestAsync(
        WorkspaceSuggestionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!agent.IsConfigured)
        {
            return Unavailable();
        }

        var answer = await AskAsync(request, cancellationToken);

        if (answer is null)
        {
            return Unavailable();
        }

        return answer.Status == SuggestionStatus.Rejected
            ? new WorkspaceSuggestion(SuggestionStatus.Rejected, [], answer.Code, [])
            : Suggested(answer);
    }

    /// <summary>
    /// What the agent answered, or null when it could not be reached.
    /// </summary>
    /// <remarks>
    /// A failure to reach the agent is not a refusal: the page says something different for it, and only
    /// one of the two invites a retry. A timeout counts as unreachable, because a customer waiting on a
    /// request that will never arrive is in the same position as one whose agent is down.
    /// </remarks>
    private async Task<AgentSuggestion?> AskAsync(WorkspaceSuggestionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await agent.AskAsync(request.Query, cancellationToken);
        }
        catch (Exception failure) when (failure is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>The candidates the catalogue can supply, with the SKUs it cannot supply removed.</summary>
    private WorkspaceSuggestion Suggested(AgentSuggestion answer)
    {
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

        return new WorkspaceSuggestion(answer.Status, options, answer.Code, dropped);
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

    private static WorkspaceSuggestion Unavailable()
        => new(SuggestionStatus.Unavailable, [], Code: null, DroppedSkus: []);
}
