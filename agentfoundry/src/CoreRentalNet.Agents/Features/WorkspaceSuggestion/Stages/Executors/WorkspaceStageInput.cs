using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Shared.Serialization;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;

/// <summary>What each stage is shown, in the stage's own terms and nothing more.</summary>
/// <remarks>
/// <para>
/// A stage that is shown something it does not need is a stage that can act on it. The verifier is shown the
/// sentence and the slot rules; the composer is shown the expansion and the retrieved products, never the
/// catalogue; the reviewer is shown the sentence, the expansion, what the searches came back with, and the
/// setups. The caller's token is never in any of these, because it is read from the invocation's own header and
/// never entered a message.
/// </para>
/// <para>
/// <b>Two of these carry a crossing the stage must not have to work out for itself.</b> The rephraser is told
/// the currency and the ceiling the application recorded, and the retriever is told the catalogue arguments for
/// each component. A model left to derive either is a model that can get it wrong once per run.
/// </para>
/// </remarks>
internal static class WorkspaceStageInput
{
    public static string Verification(WorkspaceSuggestionWorkflowState state)
        => ContractJson.Serialize(new { query = state.OriginalCustomerQuery, slots = state.SlotCapacityRules });

    /// <summary>What the previous attempt failed on, from the two places that can know.</summary>
    /// <remarks>
    /// <b>Two sources, because one of them does not always run.</b> The reviewer's issues describe a set of setups
    /// that was composed and rejected — but a set that could not be composed at all never reaches the reviewer,
    /// and the structure validator routes straight to the retry. On that path the only account of what went wrong
    /// is what the searches came back with, so it is carried here rather than only into the review.
    /// <para>
    /// Without it, the attempt that ran out of budget to retrieve anything would be retried with nothing to
    /// correct — the same silent loop the retry edge exists to avoid.
    /// </para>
    /// </remarks>
    public static string Rephrasing(WorkspaceSuggestionWorkflowState state)
        => ContractJson.Serialize(new
        {
            query = state.OriginalCustomerQuery,
            slots = state.SlotCapacityRules,
            currency = state.CatalogueCurrency,
            ceilingMonthly = state.CustomerStatedCeilingMonthly,
            previousIssues = state.PreviousAttemptIssues,
            previousSearches = state.CatalogueRetrieval?.ComponentSearchOutcomes ?? [],
        });

    /// <summary>One entry per relevant component: its words, and the tool arguments that carry them.</summary>
    /// <remarks>
    /// The mapping between a component category and a catalogue tool's own words is stated in code and handed
    /// over, so the retriever calls the tool with what it was told rather than translating the category itself.
    /// A component the customer did not ask for is not offered at all, which is the same rule the structure
    /// validator applies to a composed line.
    /// </remarks>
    public static string Retrieval(WorkspaceSuggestionWorkflowState state)
        => ContractJson.Serialize(new
        {
            searches = state.RequirementExpansion is null
                ? []
                : state.RequirementExpansion.ComponentExpansions.Every()
                    .Where(component => component.Expansion.IsRelevant)
                    .Select(component => new
                    {
                        category = component.ComponentCategory,
                        catalogCategory = WorkspaceComponentVocabularyMapping
                            .CatalogueCategoryArgumentFor(component.ComponentCategory),
                        catalogSubCategory = WorkspaceComponentVocabularyMapping
                            .CatalogueSubCategoryArgumentFor(component.ComponentCategory),
                        maximumMonthlyAmount = component.Expansion.MonthlyBudget.MaximumAmount,
                        component.Expansion.SearchTerms,
                        component.Expansion.Synonyms,
                        component.Expansion.RetrievalQuery,
                        component.Expansion.SemanticConcepts,
                    }),
        });

    /// <summary>How much of a product's description a reranking call is shown.</summary>
    /// <remarks>
    /// <b>A trim, and its number is a cost decision the pool's width forced.</b> Descriptions run to three and a
    /// half thousand characters and a pool is fifteen products per component, so untrimmed this input would spend
    /// more on prose than on everything else an attempt does. The cut is made at a word boundary, because a
    /// description cut mid-word reads as a typo and this stage is asked to judge what a product is from it.
    /// </remarks>
    private const int MaximumDescriptionCharacterCountForReranking = 320;

    /// <summary>One entry per requested component: the need it must answer, and the products a search found.</summary>
    /// <remarks>
    /// The products are the pool's rows — the tools' own values, from the recorded answers — so a description
    /// reaches this stage without a model having retyped it. Only the components the customer asked for are
    /// offered, which is the same rule the structure validator applies to a composed line.
    /// </remarks>
    public static string Reranking(WorkspaceSuggestionWorkflowState state)
        => ContractJson.Serialize(new
        {
            query = state.OriginalCustomerQuery,
            intent = state.RequirementExpansion?.WorkspaceIntent,
            components = state.RequirementExpansion is null
                ? []
                : state.RequirementExpansion.ComponentExpansions.Every()
                    .Where(component => component.Expansion.IsRelevant)
                    .Select(component => new
                    {
                        category = component.ComponentCategory,
                        need = component.Expansion.RetrievalQuery,
                        candidates = ProductsFor(state, component.ComponentCategory)
                            .Select(product => new
                            {
                                product.Sku,
                                product.Name,
                                description = DescriptionForReranking(product.Description),
                                product.Amount,
                            }),
                    }),
        });

    public static string Composition(WorkspaceSuggestionWorkflowState state)
        => ContractJson.Serialize(new
        {
            requirement = state.RequirementExpansion,
            candidates = state.SelectedWorkspaceCandidates.Select(selection => new
            {
                slot = selection.RetrievedProduct.Slot,
                selection.RetrievedProduct.Sku,
                selection.RetrievedProduct.Name,
                amount = selection.RetrievedProduct.Amount,
                selection.Relevance,
                selection.Reason,
            }),
        });

    /// <summary>The pool's products for one component.</summary>
    private static IEnumerable<RetrievedWorkspaceComponentProduct> ProductsFor(
        WorkspaceSuggestionWorkflowState state,
        WorkspaceComponentCategory componentCategory)
    {
        var slot = WorkspaceComponentVocabularyMapping.CompositionSlotFor(componentCategory);

        return state.CandidatePool.Where(product => product.Slot == slot);
    }

    /// <summary>A description cut to what a relevance judgement needs, at a word boundary.</summary>
    private static string DescriptionForReranking(string description)
    {
        if (description.Length <= MaximumDescriptionCharacterCountForReranking)
        {
            return description;
        }

        var lastWordBoundary = description.LastIndexOf(' ', MaximumDescriptionCharacterCountForReranking);

        return lastWordBoundary <= 0
            ? description[..MaximumDescriptionCharacterCountForReranking]
            : description[..lastWordBoundary];
    }

    public static string Review(WorkspaceSuggestionWorkflowState state)
        => ContractJson.Serialize(new
        {
            query = state.OriginalCustomerQuery,
            requirement = state.RequirementExpansion,
            searched = state.CatalogueRetrieval?.ComponentSearchOutcomes ?? [],
            setups = state.ProposedWorkspaceSetups,
        });
}
