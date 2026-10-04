using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Guardrails;

/// <summary>The catalogue's own vocabulary: a search must name a category the catalogue has.</summary>
/// <remarks>
/// One policy covers both catalogue tools because the two publish the same request contract, so a second class
/// would be the same check twice. If either grows a parameter the other does not have, this splits into one
/// policy per tool rather than growing a switch.
/// </remarks>
internal sealed class CatalogueSearchArgumentPolicy : IToolArgumentPolicy
{
    public bool AppliesTo(string toolName) => CatalogueSearchToolNames.All.Contains(toolName, StringComparer.Ordinal);

    public GuardrailDecision Evaluate(ToolInvocation invocation)
    {
        var category = ArgumentAsText(invocation.Arguments, "category");
        var subCategory = ArgumentAsText(invocation.Arguments, "subCategory");

        return WorkspaceComponentVocabularyMapping.CanBeSearchedBy(category, subCategory)
            ? GuardrailDecision.Allow()
            : GuardrailDecision.Deny(
                "The call was refused: the catalogue has no category "
                + $"'{category}'"
                + (subCategory is null ? "." : $" with subcategory '{subCategory}'."));
    }

    /// <summary>One argument's text, whether the loop passed a string or already-parsed JSON.</summary>
    private static string? ArgumentAsText(IReadOnlyDictionary<string, object?> arguments, string name)
        => arguments.TryGetValue(name, out var value)
            ? value switch
            {
                null => null,
                string text => text,
                System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.String } element => element.GetString(),
                _ => value.ToString(),
            }
            : null;
}
