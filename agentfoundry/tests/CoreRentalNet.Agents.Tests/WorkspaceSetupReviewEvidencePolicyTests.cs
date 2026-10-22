using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.DeterministicPolicies;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>A semantic rejection survives only when the reviewer's two quotes support it.</summary>
public sealed class WorkspaceSetupReviewEvidencePolicyTests
{
    private readonly WorkspaceSetupReviewEvidencePolicy _policy = new();

    [Fact]
    public void A_material_present_in_the_product_name_cannot_be_rejected()
    {
        var result = Apply(
            query: "I need a Slate Teak desk.",
            productName: "HON Mod Desk Shell, 60 x 30 x 29, Slate Teak",
            issue: new WorkspaceSetupReviewIssue(
                "SLOT_PURPOSE_UNSATISFIED",
                "the desk does not match Slate Teak",
                CustomerEvidence: "Slate Teak",
                ProductEvidence: "HON Mod Desk Shell, 60 x 30 x 29, Slate Teak"));

        result.IsAcceptable.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void A_colour_and_type_present_in_the_product_name_cannot_be_rejected()
    {
        var result = Apply(
            query: "I need a black standing desk.",
            productName: "STORKING Electric Height Adjustable Standing Desk (36 X 20, Black)",
            issue: new WorkspaceSetupReviewIssue(
                "SLOT_PURPOSE_UNSATISFIED",
                "the desk does not match a black standing desk",
                CustomerEvidence: "black standing desk",
                ProductEvidence: "STORKING Electric Height Adjustable Standing Desk (36 X 20, Black)"));

        result.IsAcceptable.Should().BeTrue();
    }

    [Fact]
    public void A_real_contradiction_survives()
    {
        var result = Apply(
            query: "I need a racing chair.",
            productName: "WorkPro Oceanic Mesh/Fabric Ergonomic High-Back Executive Chair",
            issue: new WorkspaceSetupReviewIssue(
                "SLOT_PURPOSE_UNSATISFIED",
                "the chair is not a racing chair",
                CustomerEvidence: "racing chair",
                ProductEvidence: "WorkPro Oceanic Mesh/Fabric Ergonomic High-Back Executive Chair"));

        result.IsAcceptable.Should().BeFalse();
        result.Issues.Should().ContainSingle();
    }

    [Fact]
    public void A_semantic_issue_without_evidence_is_dropped()
    {
        var result = Apply(
            query: "I need a desk.",
            productName: "A desk",
            issue: new WorkspaceSetupReviewIssue(
                "SLOT_PURPOSE_UNSATISFIED",
                "the desk is unsuitable"));

        result.IsAcceptable.Should().BeTrue();
    }

    [Fact]
    public void A_budget_issue_is_dropped_when_the_request_has_no_budget()
    {
        var result = Apply(
            query: "I need a desk and a chair.",
            productName: "A desk",
            issue: new WorkspaceSetupReviewIssue(
                "COMPONENT_BUDGET_EXCLUDES_ALL",
                "the chair allocation was too low"));

        result.IsAcceptable.Should().BeTrue();
    }

    [Fact]
    public void A_missing_slot_issue_is_never_weakened()
    {
        var result = Apply(
            query: "I need a desk and a chair.",
            productName: "A desk",
            issue: new WorkspaceSetupReviewIssue(
                "MISSING_SLOT",
                "the chair is missing"));

        result.IsAcceptable.Should().BeFalse();
        result.Issues.Should().ContainSingle();
    }


    [Fact]
    public void An_exact_brand_and_type_quoted_by_the_reviewer_cannot_be_rejected()
    {
        var query = "I need a compact home office with a VIVO mobile standing desk, a ZSARTS small velvet chair, and a Prinaight 3 Pack Mini Plants.";
        var productName = "VIVO White Electric Mobile Height Adjustable 36 inch Dual Platform Standing Desk with Wheels, Rolling Small Space Table, Sit Stand Workstation, DESK-V111VTW";
        var result = Apply(
            query,
            productName,
            new WorkspaceSetupReviewIssue(
                "SLOT_PURPOSE_UNSATISFIED",
                "the desk does not match",
                CustomerEvidence: "VIVO mobile standing desk",
                ProductEvidence: productName));

        result.IsAcceptable.Should().BeTrue();
    }


    [Fact]
    public void A_colour_the_description_supports_cannot_be_rejected_from_the_name_alone()
    {
        var product = new RetrievedWorkspaceComponentProduct(
            WorkspaceSlot.Desk,
            "CHA-1",
            "Leather Guest Chair",
            "Black leather chair with a padded seat.",
            100_000);
        var issue = new WorkspaceSetupReviewIssue(
            "SLOT_PURPOSE_UNSATISFIED",
            "the chair does not match",
            CustomerEvidence: "black leather chair",
            ProductEvidence: "Leather Guest Chair");

        var result = _policy.Apply(
            new WorkspaceSetupReviewResult(false, [issue], "rejected"),
            "I need a black leather chair.",
            [new WorkspaceSetupCandidate(
            [
                new WorkspaceSetupLine(WorkspaceSlot.Chair, product.Sku, product.Name, 1, product.Amount),
            ])],
            [new SelectedWorkspaceComponentProduct(product, ProductRelevanceLevel.High)],
            requirementExpansion: null);

        result.IsAcceptable.Should().BeTrue();
    }

    private WorkspaceSetupReviewResult Apply(
        string query,
        string productName,
        WorkspaceSetupReviewIssue issue)
    {
        var product = new RetrievedWorkspaceComponentProduct(
            WorkspaceSlot.Desk,
            "SKU-1",
            productName,
            productName,
            100_000);
        var selected = new[]
        {
            new SelectedWorkspaceComponentProduct(product, ProductRelevanceLevel.High),
        };
        var setups = new[]
        {
            new WorkspaceSetupCandidate(
            [
                new WorkspaceSetupLine(WorkspaceSlot.Desk, product.Sku, product.Name, 1, product.Amount),
            ]),
        };

        return _policy.Apply(
            new WorkspaceSetupReviewResult(false, [issue], "rejected"),
            query,
            setups,
            selected,
            requirementExpansion: null);
    }
}
