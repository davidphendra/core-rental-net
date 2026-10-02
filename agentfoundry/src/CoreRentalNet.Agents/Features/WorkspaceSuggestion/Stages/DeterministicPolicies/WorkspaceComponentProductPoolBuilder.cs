using System.Text.Json;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;

/// <summary>Turns what the catalogue's tools answered into the products each component may be furnished from.</summary>
/// <remarks>
/// <para>
/// <b>This is where the recorded answers pay.</b> The rows come from the tools' own text, so a product's
/// description reaches the reranker without any model having retyped it, and a SKU no search returned cannot
/// appear at all.
/// </para>
/// <para>
/// <b>Which tools are the catalogue's is given to it rather than written into it.</b> The names arrive from the
/// composition root, so a deployment that publishes a third catalogue tool adds it to one registration instead of
/// editing this class — and an answer from a tool that is not the catalogue's is simply not read.
/// </para>
/// <para>
/// The component each product belongs to is read off the <i>arguments</i> the tool was called with, not off the
/// answer: a tool knows a catalogue category, and the mapping is the one place that knows which component that is.
/// </para>
/// </remarks>
public sealed class WorkspaceComponentProductPoolBuilder(IReadOnlyList<string> catalogueSearchToolNames)
{
    private readonly IReadOnlyList<string> _catalogueSearchToolNames =
        catalogueSearchToolNames ?? throw new ArgumentNullException(nameof(catalogueSearchToolNames));

    /// <summary>Every product the recorded searches returned, in the order the searches returned them.</summary>
    public IReadOnlyList<RetrievedWorkspaceComponentProduct> BuildPoolFrom(McpToolAnswerLedger recordedToolAnswers)
    {
        ArgumentNullException.ThrowIfNull(recordedToolAnswers);

        var retrievedProducts = new List<RetrievedWorkspaceComponentProduct>();

        foreach (var recordedToolAnswer in recordedToolAnswers.RecordedAnswers
            .Where(answer => _catalogueSearchToolNames.Contains(answer.ToolName, StringComparer.Ordinal)))
        {
            var componentCategory = WorkspaceComponentVocabularyMapping.ComponentCategorySearchedBy(
                ArgumentAsText(recordedToolAnswer.Arguments, "category") ?? string.Empty,
                ArgumentAsText(recordedToolAnswer.Arguments, "subCategory"));

            var slot = WorkspaceComponentVocabularyMapping.CompositionSlotFor(componentCategory);

            foreach (var catalogueSearchToolItem in ReadToolAnswer(recordedToolAnswer.AnswerText).Matches.Value)
            {
                retrievedProducts.Add(new RetrievedWorkspaceComponentProduct(
                    slot,
                    catalogueSearchToolItem.Sku,
                    catalogueSearchToolItem.Name,
                    catalogueSearchToolItem.Description,
                    catalogueSearchToolItem.PricePerMonth));
            }
        }

        return retrievedProducts;
    }

    /// <summary>One tool answer, read through the contract the tools publish it in.</summary>
    /// <remarks>
    /// It refuses rather than returning nothing. An answer from a catalogue tool that cannot be read means the two
    /// solutions' contract has diverged, and a run that quietly reranked an empty pool would report a catalogue
    /// with nothing in it.
    /// </remarks>
    private static CatalogueSearchToolAnswer ReadToolAnswer(string answerText)
        => JsonSerializer.Deserialize<CatalogueSearchToolAnswer>(answerText, ContractJson.Options)
            ?? throw new InvalidOperationException(
                "A catalogue search answered with nothing this reader could read, so the tool's contract and the "
                + "agent's have diverged.");

    /// <summary>One argument's text, whether the function loop passed a string or already-parsed JSON.</summary>
    private static string? ArgumentAsText(IReadOnlyDictionary<string, object?> toolArguments, string argumentName)
        => toolArguments.TryGetValue(argumentName, out var argumentValue)
            ? argumentValue switch
            {
                null => null,
                string text => text,
                JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
                JsonElement { ValueKind: JsonValueKind.Null } => null,
                _ => argumentValue.ToString(),
            }
            : null;
}
