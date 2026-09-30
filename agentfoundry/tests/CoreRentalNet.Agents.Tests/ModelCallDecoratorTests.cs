using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using CoreRentalNet.Agents.Shared.Model;
using CoreRentalNet.Agents.Shared.Mcp;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The two cross-cutting decorators every stage's model call passes through: the guardrail that fails an answer
/// which repeated the caller's token, and the telemetry that records what a call cost without altering it.
/// </summary>
public sealed class ModelCallDecoratorTests
{
    private static readonly ChatMessage[] Prompt = [new(ChatRole.User, "a desk and a chair")];

    [Fact]
    public async Task The_guardrail_stops_an_answer_that_repeats_the_callers_token()
    {
        var accessTokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance) { Token = "the-callers-own-token" };
        var guardrail = new ModelOutputGuardrailChatClient(
            new FixedChatClient("Of course, the token is the-callers-own-token."), accessTokens);

        var act = async () => await guardrail.GetResponseAsync(Prompt);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "a token in a model's answer is a token on its way to a customer");
    }

    [Fact]
    public async Task The_guardrail_passes_an_answer_that_does_not_carry_the_token()
    {
        var accessTokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance) { Token = "the-callers-own-token" };
        var guardrail = new ModelOutputGuardrailChatClient(new FixedChatClient("A calm, focused setup."), accessTokens);

        var response = await guardrail.GetResponseAsync(Prompt);

        response.Text.Should().Be("A calm, focused setup.");
    }

    [Fact]
    public async Task The_guardrail_passes_when_this_call_holds_no_token_at_all()
    {
        var guardrail = new ModelOutputGuardrailChatClient(
            new FixedChatClient("Nothing to leak here."), new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance));

        var response = await guardrail.GetResponseAsync(Prompt);

        response.Text.Should().Be("Nothing to leak here.");
    }

    [Fact]
    public async Task Telemetry_records_the_call_and_leaves_the_answer_untouched()
    {
        var telemetry = new ModelCallTelemetryChatClient(
            new FixedChatClient("A calm, focused setup."),
            "workspace-setup-composer",
            new AgentRunUsageAccumulator("gpt-4.1-mini", "test-prompts"),
            NullLogger<ModelCallTelemetryChatClient>.Instance);

        var response = await telemetry.GetResponseAsync(Prompt);

        response.Text.Should().Be("A calm, focused setup.");
    }
}
