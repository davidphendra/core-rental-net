using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The two guarantees a reranker's answer is read under: it cannot introduce a product, and it cannot relabel one.
/// </summary>
/// <remarks>
/// Both are what let the rest of the pipeline treat the reranker's answer as a preference rather than as data. A
/// SKU it was not given is dropped, the slot of everything it kept is the pool's, and a product it did not mention
/// is a product it rejected — appending the rest would make the ranking advisory and put the pool's width back in
/// front of the composer.
/// </remarks>
public sealed class WorkspaceComponentProductSelectionPolicyTests
{
    private static readonly WorkspaceComponentProductSelectionPolicy SelectionPolicy = new();

    [Fact]
    public void A_product_the_reranker_was_given_is_kept_with_its_reason_and_its_level()
    {
        var selected = SelectionPolicy.SelectFromPool(
            [Desk("DSK-1", "Sit-Stand Desk")],
            Ranking(desk: [("DSK-1", ProductRelevanceLevel.High, "a surface that rises")]));

        selected.Should().ContainSingle();
        selected[0].RetrievedProduct.Sku.Should().Be("DSK-1");
        selected[0].Relevance.Should().Be(ProductRelevanceLevel.High);
        selected[0].Reason.Should().Be("a surface that rises");
    }

    [Fact] // the guard that stops a model inventing a product
    public void A_sku_the_reranker_was_not_given_is_dropped()
    {
        var selected = SelectionPolicy.SelectFromPool(
            [Desk("DSK-1", "Sit-Stand Desk")],
            Ranking(desk: [("DSK-INVENTED", ProductRelevanceLevel.High, "a lovely desk")]));

        selected.Should().BeEmpty("a product a search did not return cannot be composed from");
    }

    [Fact] // the row the composer reads is the pool's, so a description or a price cannot be rewritten
    public void The_product_is_the_pools_own_row_and_not_the_models_restatement()
    {
        var pool = new RetrievedWorkspaceComponentProduct(WorkspaceSlot.Desk, "DSK-1", "Sit-Stand Desk", "Solid oak.", 100m);

        var selected = SelectionPolicy.SelectFromPool(
            [pool],
            Ranking(desk: [("DSK-1", ProductRelevanceLevel.High, "a surface that rises")]));

        selected[0].RetrievedProduct.Should().Be(pool);
    }

    [Fact] // a component the reranker said nothing useful about is a component with nothing to compose from
    public void A_product_the_reranker_did_not_mention_is_rejected()
    {
        var selected = SelectionPolicy.SelectFromPool(
            [Desk("DSK-1", "Sit-Stand Desk"), Desk("DSK-2", "Plain Desk")],
            Ranking(desk: [("DSK-1", ProductRelevanceLevel.High, "a surface that rises")]));

        selected.Should().ContainSingle();
        selected[0].RetrievedProduct.Sku.Should().Be("DSK-1");
    }

    [Fact] // three per component, so fifteen candidates cannot become fifteen rows of the composer's context
    public void At_most_the_bound_is_kept_for_each_component()
    {
        var pool = Enumerable.Range(1, 6).Select(position => Desk($"DSK-{position}", $"Desk {position}")).ToList();
        var ranking = Ranking(desk: [.. pool.Select(product => (product.Sku, ProductRelevanceLevel.High, "a desk"))]);

        SelectionPolicy.SelectFromPool(pool, ranking).Should().HaveCount(3);
    }

    [Fact] // the ranking is the order, so the order is what the composer reads first
    public void The_order_the_reranker_gave_is_the_order_kept()
    {
        var selected = SelectionPolicy.SelectFromPool(
            [Desk("DSK-1", "First"), Desk("DSK-2", "Second")],
            Ranking(desk:
            [
                ("DSK-2", ProductRelevanceLevel.High, "best"),
                ("DSK-1", ProductRelevanceLevel.Medium, "second best"),
            ]));

        selected.Select(selection => selection.RetrievedProduct.Sku).Should().Equal("DSK-2", "DSK-1");
    }

    [Fact] // a component the reranker returned nothing for is not one the search failed at
    public void A_component_with_an_empty_list_selects_nothing()
    {
        SelectionPolicy.SelectFromPool([Desk("DSK-1", "Sit-Stand Desk")], Ranking(desk: []))
            .Should().BeEmpty();
    }

    private static RetrievedWorkspaceComponentProduct Desk(string sku, string name)
        => new(WorkspaceSlot.Desk, sku, name, "A desk.", 100m);

    /// <summary>A reranker's answer with one component filled in and the other six empty.</summary>
    private static WorkspaceComponentProductRankingResult Ranking(
        IReadOnlyList<(string Sku, ProductRelevanceLevel Relevance, string Reason)> desk)
    {
        static IReadOnlyList<WorkspaceComponentProductAssessment> Assessed(
            IReadOnlyList<(string Sku, ProductRelevanceLevel Relevance, string Reason)> assessments)
            => [.. assessments.Select(assessment =>
                new WorkspaceComponentProductAssessment(assessment.Sku, assessment.Relevance, assessment.Reason))];

        return new WorkspaceComponentProductRankingResult(new WorkspaceComponentProductAssessments(
            Desk: Assessed(desk),
            Chair: [],
            Monitor: [],
            Lamp: [],
            Plant: [],
            BeanBag: [],
            CoffeeMachine: []));
    }
}
