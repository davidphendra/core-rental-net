using System.Text.Json;

namespace CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;

/// <summary>Turns the compact model answer into the seven-category domain expansion.</summary>
/// <remarks>
/// <para>
/// The wire contract is sparse because sending five irrelevant categories through the model costs hundreds of
/// output tokens. The domain contract is dense because every downstream stage walks one fixed vocabulary. This
/// mapper is the one place those two shapes meet.
/// </para>
/// <para>
/// Validation is deliberately strict: a repeated category, an allocation for a category that was not returned,
/// or an allocation total above the stated total is a contract failure rather than a value to guess at. A
/// customer's explicit category that the model omitted is also refused, because omission now means irrelevant.
/// An allocation returned without a total is ignored: the budget invariant downstream already clears invented
/// money, and a cosmetic slip must not end the run.
/// </para>
/// </remarks>
internal static class WorkspaceRequirementExpansionMapper
{
    public static WorkspaceRequirementExpansion Map(
        WorkspaceRequirementExpansionWire expansion,
        string originalCustomerQuery)
    {
        ArgumentNullException.ThrowIfNull(expansion);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalCustomerQuery);

        var categories = ReadCategories(expansion.ComponentExpansions);
        var allocations = ReadAllocations(expansion, categories.Keys);

        return new WorkspaceRequirementExpansion(
            OriginalCustomerQuery: originalCustomerQuery,
            WorkspaceIntent: MapIntent(expansion.WorkspaceIntent),
            TotalMonthlyBudget: expansion.TotalMonthlyBudget,
            ComponentExpansions: new WorkspaceComponentExpansions(
                Desk: ComponentFor(WorkspaceComponentCategory.Desk, categories, allocations, expansion),
                Chair: ComponentFor(WorkspaceComponentCategory.Chair, categories, allocations, expansion),
                Monitor: ComponentFor(WorkspaceComponentCategory.Monitor, categories, allocations, expansion),
                Lamp: ComponentFor(WorkspaceComponentCategory.Lamp, categories, allocations, expansion),
                Plant: ComponentFor(WorkspaceComponentCategory.Plant, categories, allocations, expansion),
                BeanBag: ComponentFor(WorkspaceComponentCategory.BeanBag, categories, allocations, expansion),
                CoffeeMachine: ComponentFor(
                    WorkspaceComponentCategory.CoffeeMachine, categories, allocations, expansion)));
    }

    private static Dictionary<WorkspaceComponentCategory, WorkspaceComponentExpansionWire> ReadCategories(
        IReadOnlyList<WorkspaceComponentExpansionWire>? wireCategories)
    {
        if (wireCategories is null)
        {
            throw new JsonException("The rephraser answer has no categories.");
        }

        var categories = new Dictionary<WorkspaceComponentCategory, WorkspaceComponentExpansionWire>();

        foreach (var category in wireCategories)
        {
            if (!categories.TryAdd(category.ComponentCategory, category))
            {
                throw new JsonException(
                    $"The rephraser answer repeats the category '{category.ComponentCategory}'.");
            }
        }

        return categories;
    }

    private static Dictionary<WorkspaceComponentCategory, int> ReadAllocations(
        WorkspaceRequirementExpansionWire expansion,
        IEnumerable<WorkspaceComponentCategory> presentCategories)
    {
        var allocations = new Dictionary<WorkspaceComponentCategory, int>();
        var wireAllocations = expansion.Allocations ?? [];

        if (wireAllocations.Count == 0 || expansion.TotalMonthlyBudget.Amount is null)
        {
            // A model can echo the allocation example even when it left the total null. The budget invariant
            // clears invented money downstream; refusing here would turn a cosmetic slip into a failed run.
            return allocations;
        }

        var total = expansion.TotalMonthlyBudget.Amount.Value;
        var present = presentCategories.ToHashSet();

        foreach (var allocation in wireAllocations)
        {
            AddAllocation(allocation, present, allocations);
        }

        var sum = allocations.Values.Sum(amount => (long)amount);

        if (sum > total)
        {
            throw new JsonException($"The rephraser allocated {sum}; the stated total is {total}.");
        }

        return allocations;
    }

    private static void AddAllocation(
        WorkspaceComponentAllocationWire allocation,
        IReadOnlySet<WorkspaceComponentCategory> present,
        Dictionary<WorkspaceComponentCategory, int> allocations)
    {
        if (allocation.Amount < 0)
        {
            throw new JsonException(
                $"The rephraser allocated a negative amount to '{allocation.ComponentCategory}'.");
        }

        if (!present.Contains(allocation.ComponentCategory))
        {
            throw new JsonException(
                $"The rephraser allocated to the omitted category '{allocation.ComponentCategory}'.");
        }

        if (!allocations.TryAdd(allocation.ComponentCategory, allocation.Amount))
        {
            throw new JsonException(
                $"The rephraser allocated twice to '{allocation.ComponentCategory}'.");
        }
    }

    private static WorkspaceIntent MapIntent(WorkspaceIntentWire intent)
        => new(
            PurposePhrases: intent.PurposePhrases ?? [],
            StylePhrases: intent.StylePhrases ?? [],
            ExperiencePhrases: intent.ExperiencePhrases ?? [],
            UsagePhrases: intent.UsagePhrases ?? []);

    private static WorkspaceComponentExpansion ComponentFor(
        WorkspaceComponentCategory category,
        IReadOnlyDictionary<WorkspaceComponentCategory, WorkspaceComponentExpansionWire> categories,
        IReadOnlyDictionary<WorkspaceComponentCategory, int> allocations,
        WorkspaceRequirementExpansionWire expansion)
    {
        if (!categories.TryGetValue(category, out var wireCategory))
        {
            return Irrelevant();
        }

        var budget = allocations.TryGetValue(category, out var amount)
            ? new WorkspaceComponentMonthlyBudget(
                MaximumAmount: amount,
                Currency: expansion.TotalMonthlyBudget.Currency,
                IsExplicit: false,
                IsDerived: true)
            : new WorkspaceComponentMonthlyBudget(null, null, IsExplicit: false, IsDerived: false);

        return new WorkspaceComponentExpansion(
            IsRelevant: true,
            RetrievalQuery: wireCategory.RetrievalQuery,
            SearchTerms: wireCategory.SearchTerms ?? [],
            Synonyms: wireCategory.Synonyms ?? [],
            SemanticConcepts: wireCategory.SemanticConcepts ?? [],
            MonthlyBudget: budget);
    }

    private static WorkspaceComponentExpansion Irrelevant()
        => new(
            IsRelevant: false,
            RetrievalQuery: string.Empty,
            SearchTerms: [],
            Synonyms: [],
            SemanticConcepts: [],
            MonthlyBudget: new WorkspaceComponentMonthlyBudget(null, null, IsExplicit: false, IsDerived: false));
}
