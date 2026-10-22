using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Outputs;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Stages.Executors;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The reviewer must be shown the product description it is asked to judge.</summary>
public sealed class WorkspaceStageInputReviewTests
{
    [Fact]
    public void The_review_input_carries_each_selected_products_description()
    {
        var product = new RetrievedWorkspaceComponentProduct(
            WorkspaceSlot.Desk,
            "DSKB08XN4JDR",
            "HON Mod Desk Shell, Mahogany",
            "A classic mahogany desk with a sturdy work surface.",
            266_000);

        var state = new WorkspaceSuggestionWorkflowState
        {
            CustomerWorkflowIdentifier = "run-1",
            OriginalCustomerQuery = "I want a classic mahogany desk for my home office.",
            SelectedWorkspaceCandidates =
            [
                new SelectedWorkspaceComponentProduct(product, ProductRelevanceLevel.High),
            ],
            ProposedWorkspaceSetups =
            [
                new WorkspaceSetupCandidate(
                [
                    new WorkspaceSetupLine(
                        WorkspaceSlot.Desk,
                        product.Sku,
                        product.Name,
                        Quantity: 1,
                        Amount: product.Amount),
                ]),
            ],
        };

        using var review = JsonDocument.Parse(WorkspaceStageInput.Review(state));

        var line = review.RootElement
            .GetProperty("setups")[0]
            .GetProperty("lines")[0];

        line.GetProperty("description").GetString().Should().Be(product.Description);
        review.RootElement.GetProperty("query").GetString().Should().Be(state.OriginalCustomerQuery);
    }

    [Fact]
    public void A_setup_line_without_a_selected_product_has_no_description()
    {
        var state = new WorkspaceSuggestionWorkflowState
        {
            CustomerWorkflowIdentifier = "run-1",
            OriginalCustomerQuery = "a desk",
            ProposedWorkspaceSetups =
            [
                new WorkspaceSetupCandidate(
                [
                    new WorkspaceSetupLine(
                        WorkspaceSlot.Desk,
                        "UNKNOWN",
                        "Unknown desk",
                        Quantity: 1,
                        Amount: 1),
                ]),
            ],
        };

        using var review = JsonDocument.Parse(WorkspaceStageInput.Review(state));

        var description = review.RootElement
            .GetProperty("setups")[0]
            .GetProperty("lines")[0]
            .GetProperty("description");

        description.ValueKind.Should().Be(JsonValueKind.Null);
    }
}
