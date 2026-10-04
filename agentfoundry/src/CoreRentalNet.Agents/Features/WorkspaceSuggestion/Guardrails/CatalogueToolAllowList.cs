using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Guardrails;

/// <summary>The catalogue's two search tools, from the one constant both the offer filter and the guard share.</summary>
internal sealed class CatalogueToolAllowList : IToolAllowList
{
    public bool Contains(string toolName)
        => CatalogueSearchToolNames.All.Contains(toolName, StringComparer.Ordinal);
}
