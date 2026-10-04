using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;

/// <summary>The one door between a component category, a composition slot and a catalogue tool's argument.</summary>
/// <remarks>
/// <para>
/// <b>It is a translator, not a lookup table.</b> The agent reaches the catalogue over MCP and holds no reference
/// to the module that publishes it, so the category and subcategory a tool is called with are that tool's own
/// words and not a type this solution can depend on.
/// </para>
/// <para>
/// Stating the crossing once is what stops it being restated in a prompt, in a stage input and in a validator —
/// three places that would then be able to disagree. A new component category is three lines here and nothing
/// anywhere else.
/// </para>
/// </remarks>
public static class WorkspaceComponentVocabularyMapping
{
    /// <summary>The composition slot this component category fills.</summary>
    public static WorkspaceSlot CompositionSlotFor(WorkspaceComponentCategory componentCategory)
        => componentCategory switch
        {
            WorkspaceComponentCategory.Desk => WorkspaceSlot.Desk,
            WorkspaceComponentCategory.Chair => WorkspaceSlot.Chair,
            WorkspaceComponentCategory.Monitor => WorkspaceSlot.Monitor,
            WorkspaceComponentCategory.Lamp => WorkspaceSlot.Lamp,
            WorkspaceComponentCategory.Plant => WorkspaceSlot.Plant,
            WorkspaceComponentCategory.BeanBag => WorkspaceSlot.RelaxZone,
            WorkspaceComponentCategory.CoffeeMachine => WorkspaceSlot.CoffeeStation,
            _ => throw new ArgumentOutOfRangeException(
                nameof(componentCategory), componentCategory, "Unknown workspace component category."),
        };

    /// <summary>The catalogue category a search is narrowed to for this component category.</summary>
    public static string CatalogueCategoryArgumentFor(WorkspaceComponentCategory componentCategory)
        => componentCategory switch
        {
            WorkspaceComponentCategory.Desk => "desk",
            WorkspaceComponentCategory.Chair => "chair",
            _ => "accessory",
        };

    /// <summary>The component category a catalogue search was made for, read off its own arguments.</summary>
    /// <remarks>
    /// <b>The other direction, and the reason the mapping is one class.</b> The forward pair writes the arguments
    /// the retriever sends; this reads them back off the recorded call, so the products a tool answered belong to
    /// the component that asked for them without any model restating which. A search made with arguments that
    /// name no component is refused rather than guessed at.
    /// </remarks>
    public static WorkspaceComponentCategory ComponentCategorySearchedBy(
        string catalogueCategory,
        string? catalogueSubCategory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogueCategory);

        return (catalogueCategory, catalogueSubCategory) switch
        {
            ("desk", _) => WorkspaceComponentCategory.Desk,
            ("chair", _) => WorkspaceComponentCategory.Chair,
            ("accessory", "monitor") => WorkspaceComponentCategory.Monitor,
            ("accessory", "lamp") => WorkspaceComponentCategory.Lamp,
            ("accessory", "plant") => WorkspaceComponentCategory.Plant,
            ("accessory", "beanbag") => WorkspaceComponentCategory.BeanBag,
            ("accessory", "coffee") => WorkspaceComponentCategory.CoffeeMachine,
            _ => throw new ArgumentOutOfRangeException(
                nameof(catalogueCategory),
                (catalogueCategory, catalogueSubCategory),
                "No workspace component is searched for by those arguments."),
        };
    }

    /// <summary>Whether a pair of catalogue arguments names a component a search may be made for.</summary>
    /// <remarks>
    /// The non-throwing twin of <see cref="ComponentCategorySearchedBy"/>: a guardrail wants the answer, and a
    /// refused call is a reason returned to the model rather than an exception that ends the stage.
    /// </remarks>
    public static bool CanBeSearchedBy(string? catalogueCategory, string? catalogueSubCategory)
        => (catalogueCategory, catalogueSubCategory) switch
        {
            ("desk", _) or ("chair", _) => true,
            ("accessory", "monitor" or "lamp" or "plant" or "beanbag" or "coffee") => true,
            _ => false,
        };

    /// <summary>The accessory subcategory a search is narrowed to, or null when the category stands alone.</summary>
    public static string? CatalogueSubCategoryArgumentFor(WorkspaceComponentCategory componentCategory)
        => componentCategory switch
        {
            WorkspaceComponentCategory.Desk or WorkspaceComponentCategory.Chair => null,
            WorkspaceComponentCategory.Monitor => "monitor",
            WorkspaceComponentCategory.Lamp => "lamp",
            WorkspaceComponentCategory.Plant => "plant",
            WorkspaceComponentCategory.BeanBag => "beanbag",
            WorkspaceComponentCategory.CoffeeMachine => "coffee",
            _ => throw new ArgumentOutOfRangeException(
                nameof(componentCategory), componentCategory, "Unknown workspace component category."),
        };
}
