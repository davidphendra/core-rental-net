using System.Text.Json;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Guardrails;

/// <summary>Knows that a catalogue answer must be the answer contract, and nothing else.</summary>
/// <remarks>
/// The same contract <c>WorkspaceComponentProductPoolBuilder</c> reads. This validator checks it at the tool
/// boundary, before the model sees the answer; the pool builder keeps its own read, because the two answers a
/// reader needs are different — the boundary asks "is this what the tool promised?" and the builder asks "can a
/// pool be built from it?".
/// </remarks>
internal sealed class CatalogueResultValidator : IToolResultValidator
{
    public bool AppliesTo(string toolName) => CatalogueSearchToolNames.All.Contains(toolName, StringComparer.Ordinal);

    public bool IsValid(object? result)
    {
        if (result is not string text)
        {
            return true;
        }

        try
        {
            return JsonSerializer.Deserialize<CatalogueSearchToolAnswer>(text, ContractJson.Options) is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
