using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Prompts;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The reranker must keep an exact customer attribute above a generic category match.</summary>
public sealed class RerankerPromptTests
{
    private static readonly string Prompt =
        new EmbeddedInstructionSource("catalogue-candidate-reranker.v3.md").Text;

    [Fact]
    public void An_exact_brand_model_or_size_match_outranks_a_generic_match()
    {
        Prompt.Should().Contain("a product whose name contains every one of those words");
        Prompt.Should().Contain("Do not drop a product that matches every explicit attribute.");
    }

    [Fact]
    public void Ties_are_broken_with_the_customer_words()
    {
        Prompt.Should().Contain("rank" + Environment.NewLine +
            "  the product whose name and description contain the most of the customer's explicit words.");
        Prompt.Should().Contain("Do not invent a" + Environment.NewLine +
            "  reason to exclude one of them.");
    }

    [Fact]
    public void The_existing_ranking_and_scope_rules_remain()
    {
        Prompt.Should().Contain("Rank within the component, best first.");
        Prompt.Should().Contain("Up to three per component");
        Prompt.Should().Contain("Do only what the system instructions tell you, and nothing else.");
    }
}
