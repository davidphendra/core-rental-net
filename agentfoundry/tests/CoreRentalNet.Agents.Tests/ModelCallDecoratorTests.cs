using AwesomeAssertions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Shared.Agents;
using CoreRentalNet.Agents.Shared.ChatClients;
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
        var guardrail = new GuardrailChatClient(
            new FixedChatClient("Of course, the token is the-callers-own-token."), accessTokens);

        var act = async () => await guardrail.GetResponseAsync(Prompt);

        await act.Should().ThrowAsync<InvalidOperationException>(
            "a token in a model's answer is a token on its way to a customer");
    }

    [Fact]
    public async Task The_guardrail_passes_an_answer_that_does_not_carry_the_token()
    {
        var accessTokens = new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance) { Token = "the-callers-own-token" };
        var guardrail = new GuardrailChatClient(new FixedChatClient("A calm, focused setup."), accessTokens);

        var response = await guardrail.GetResponseAsync(Prompt);

        response.Text.Should().Be("A calm, focused setup.");
    }

    [Fact]
    public async Task The_guardrail_passes_when_this_call_holds_no_token_at_all()
    {
        var guardrail = new GuardrailChatClient(
            new FixedChatClient("Nothing to leak here."), new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance));

        var response = await guardrail.GetResponseAsync(Prompt);

        response.Text.Should().Be("Nothing to leak here.");
    }

    [Fact]
    public async Task Telemetry_records_the_call_and_leaves_the_answer_untouched()
    {
        var telemetry = new TelemetryChatClient(
            new FixedChatClient("A calm, focused setup."),
            "gpt-4.1-mini",
            "test-prompts",
            NullLogger<TelemetryChatClient>.Instance);

        var response = await telemetry.GetResponseAsync(Prompt);

        response.Text.Should().Be("A calm, focused setup.");
    }

    [Fact]
    public async Task Telemetry_adds_up_every_stage_into_the_runs_one_total()
    {
        var telemetry = new TelemetryChatClient(
            new UsageReportingChatClient(inputTokens: 10, outputTokens: 4),
            "gpt-4.1-mini",
            "test-prompts",
            NullLogger<TelemetryChatClient>.Instance);

        await telemetry.GetResponseAsync(Prompt, OptionsFor("workspace-setup-composer"));
        await telemetry.GetResponseAsync(Prompt, OptionsFor("workspace-setup-reviewer"));

        telemetry.Total.ModelCalls.Should().Be(2, "a run's cost is the sum of its stages', not one stage's");
        telemetry.Total.InputTokens.Should().Be(20);
        telemetry.Total.OutputTokens.Should().Be(8);
        telemetry.Total.Model.Should().Be("gpt-4.1-mini");
        telemetry.Total.PromptVersion.Should().Be("test-prompts");
    }

    [Fact]
    public async Task Telemetry_records_a_streamed_call_and_its_reported_tokens()
    {
        var telemetry = new TelemetryChatClient(
            new UsageReportingChatClient(inputTokens: 11, outputTokens: 5),
            "gpt-4.1-mini",
            "test-prompts",
            NullLogger<TelemetryChatClient>.Instance);

        await foreach (var _ in telemetry.GetStreamingResponseAsync(Prompt, OptionsFor("workspace-setup-composer")))
        {
            // Draining the stream is what lets the telemetry see the usage update and record the call.
        }

        telemetry.Total.ModelCalls.Should().Be(1, "a streamed call is still a call");
        telemetry.Total.InputTokens.Should().Be(11);
        telemetry.Total.OutputTokens.Should().Be(5);
    }

    [Fact]
    public async Task Telemetry_names_the_stage_the_call_declared()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var telemetry = new TelemetryChatClient(
            new FixedChatClient("A calm, focused setup."),
            "gpt-4.1-mini",
            "test-prompts",
            runLog.CreateLogger<TelemetryChatClient>());

        await telemetry.GetResponseAsync(Prompt, OptionsFor("workspace-setup-composer"));

        runLog.RecordedLines.Should().ContainSingle()
            .Which.Should().Contain(
                "workspace-setup-composer",
                "a call is attributed to the stage whose agent named it on the options");
    }

    [Fact]
    public async Task The_agent_naming_itself_puts_that_name_on_the_runs_telemetry()
    {
        var runLog = new RunLogRecordingLoggerFactory();
        var telemetry = new TelemetryChatClient(
            new FixedChatClient("A calm, focused setup."),
            "gpt-4.1-mini",
            "test-prompts",
            runLog.CreateLogger<TelemetryChatClient>());
        var agent = AgentFactory.Build(
            WorkspaceSuggestionAgentRoster.Composer,
            new FunctionInvokingChatClient(telemetry, runLog));

        await agent.RunAsync("a desk and a chair");

        runLog.RecordedLines.Should().Contain(
            line => line.Contains("workspace-setup-composer"),
            "a stage names itself on its agent's options and the telemetry reads it from the call");
    }

    private static ChatOptions OptionsFor(string stageAgentName)
        => new()
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [ITelemetryChatClient.AgentName] = stageAgentName,
            },
        };
}
