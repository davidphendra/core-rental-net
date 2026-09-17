using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Prompts;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using AwesomeAssertions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The verifier's gate, against a model that says what it is told.
/// </summary>
/// <remarks>
/// What the model answers is the least interesting part. The interesting parts are what it is shown, and
/// what the code does with an answer it should not trust.
/// </remarks>
public sealed class IntentClassifierTests
{
    private static IntentClassifier Classifier(ScriptedChatClient model)
        => new(model, PromptLibrary.Beside(AppContext.BaseDirectory));

    [Fact]
    public async Task A_workspace_request_is_answered_with_the_one_code_the_contract_allows()
    {
        var verdict = await Classifier(new ScriptedChatClient("""{"isWorkspaceRequest": true, "code": "ok"}"""))
            .ClassifyAsync("two desks and some plants");

        verdict.IsWorkspaceRequest.Should().BeTrue();
        verdict.Code.Should().Be(ReasonCodes.Ok);
    }

    /// <summary>
    /// A refusal carries the contract's one code, whatever code the model offered.
    /// </summary>
    /// <remarks>
    /// The sharpest case in the whole design. The result schema's <c>code</c> enum holds exactly one
    /// value, and the workflow writes the verdict's code straight into the result - so a model that
    /// answered with a word of its own would produce a result the contract forbids, and that schema is
    /// the only thing the application and the agent share. The word below is a perfectly good word.
    /// </remarks>
    [Theory]
    [InlineData("too_vague")]
    [InlineData("not about furniture")]
    [InlineData("not_workspace_request")]
    [InlineData("")]
    public async Task A_refusal_carries_the_contracts_code_and_not_the_models(string offered)
    {
        var model = new ScriptedChatClient(
            $$"""{"isWorkspaceRequest": false, "code": "{{offered}}"}""");

        var verdict = await Classifier(model).ClassifyAsync("what is the weather in Bali");

        verdict.IsWorkspaceRequest.Should().BeFalse();
        verdict.Code.Should().Be(ReasonCodes.NotWorkspaceRequest);
    }

    /// <summary>An answer with no verdict in it is refused rather than read as a no.</summary>
    /// <remarks>
    /// Treating a broken answer as a refusal would refuse a customer on the strength of something the
    /// model never said, and nothing about that would be visible.
    /// </remarks>
    [Theory]
    [InlineData("""{"code": "ok"}""")]
    [InlineData("""{"isWorkspaceRequest": "yes"}""")]
    [InlineData("{}")]
    public async Task An_answer_without_a_verdict_is_refused(string answer)
    {
        var classify = async () => await Classifier(new ScriptedChatClient(answer)).ClassifyAsync("a desk");

        await classify.Should().ThrowAsync<PromptAnswerException>();
    }

    [Theory]
    [InlineData("no answer at all")]
    [InlineData("{ not json")]
    public async Task An_answer_that_is_not_json_is_refused(string answer)
    {
        var classify = async () => await Classifier(new ScriptedChatClient(answer)).ClassifyAsync("a desk");

        await classify.Should().ThrowAsync<PromptAnswerException>();
    }

    [Fact]
    public async Task An_answer_wrapped_in_a_sentence_is_read()
    {
        var model = new ScriptedChatClient(
            """Certainly! Here is the answer: {"isWorkspaceRequest": true, "code": "ok"} — let me know.""");

        var verdict = await Classifier(model).ClassifyAsync("a standing desk");

        verdict.IsWorkspaceRequest.Should().BeTrue();
    }

    /// <summary>The customer's words reach the model inside the markers, and nothing else does.</summary>
    /// <remarks>
    /// The gate is given the request and no catalogue, so this also asserts what is <em>not</em> in the
    /// prompt: no slot rules, no findings.
    /// </remarks>
    [Fact]
    public async Task The_model_sees_the_request_between_the_markers_and_nothing_else()
    {
        var model = new ScriptedChatClient("""{"isWorkspaceRequest": true, "code": "ok"}""");

        await Classifier(model).ClassifyAsync("two monitors please");

        model.Asked.Should().NotBeNull();

        var asked = model.Asked!;

        asked.Should().Contain("two monitors please");
        asked.Should().NotContain("{{");

        var nonce = System.Text.RegularExpressions.Regex.Match(asked, @"<<data-([0-9a-f]+)>>").Groups[1].Value;

        nonce.Should().NotBeEmpty();
        asked.IndexOf("two monitors please", StringComparison.Ordinal)
            .Should().BeLessThan(asked.IndexOf($"<<end-{nonce}>>", StringComparison.Ordinal));
    }
}
