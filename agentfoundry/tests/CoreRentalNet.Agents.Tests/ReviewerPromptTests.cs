using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Prompts;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The reviewer is grounded in the customer's sentence, not in inferred search language.</summary>
public sealed class ReviewerPromptTests
{
    private static readonly string Prompt =
        new EmbeddedInstructionSource("workspace-setup-reviewer.v3.md").Text;

    [Fact]
    public void The_customer_sentence_is_declared_the_only_source_of_hard_requirements()
    {
        Prompt.Should().Contain("The customer sentence is the only source of hard requirements.");
        Prompt.Should().Contain("The requirement expansion is a search aid.");
    }

    [Fact]
    public void Only_explicitly_requested_components_are_required()
    {
        Prompt.Should().Contain("every component explicitly requested by the customer is present");
        Prompt.Should().Contain("Ignore components that appear only in the requirement expansion.");
    }

    [Fact]
    public void The_prompt_contains_both_an_acceptable_and_an_unacceptable_example()
    {
        Prompt.Should().Contain("\"isAcceptable\": true");
        Prompt.Should().Contain("\"isAcceptable\": false");
        Prompt.Should().Contain("MISSING_SLOT");
    }

    [Fact]
    public void The_prompt_does_not_repeat_the_invented_long_session_example()
    {
        Prompt.Should().NotContain("long sessions");
        Prompt.Should().NotContain("seating does not answer");
    }

    [Fact]
    public void A_requested_material_present_in_the_product_name_must_be_treated_as_satisfied()
    {
        Prompt.Should().Contain("If the product name or description contains the requested attribute");
        Prompt.Should().Contain("Slate Teak");
        Prompt.Should().Contain("The material requirement is satisfied.");
    }

    [Fact]
    public void A_rejection_must_quote_the_customer_attribute_and_the_product_evidence()
    {
        Prompt.Should().Contain("quote both sides of the contradiction in `customerEvidence` and `productEvidence`");
        Prompt.Should().Contain("If both cannot be quoted, do not reject.");
    }

    [Fact]
    public void The_reviewer_is_told_to_use_the_product_description()
    {
        Prompt.Should().Contain("using the product's name and description");
        Prompt.Should().Contain("Do not infer that an attribute is missing");
    }

    [Fact]
    public void An_unrequested_component_is_a_review_failure()
    {
        Prompt.Should().Contain("A setup must not contain a component the customer did not name.");
        Prompt.Should().Contain("`UNREQUESTED_SLOT`");
    }

    [Fact]
    public void Substring_evidence_is_decisive_for_an_explicit_attribute()
    {
        Prompt.Should().Contain("Substring evidence is decisive.");
        Prompt.Should().Contain("Standing Desk ... Black");
    }
}
