using AwesomeAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CoreRentalNet.Agents.Shared.Mcp;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using Xunit;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Requests;

namespace CoreRentalNet.Agents.Tests;

/// <summary>
/// The call's own token, and the catalogue it entitles, applied at the model call.
/// </summary>
/// <remarks>
/// This is the work that used to be an <c>AIAgent</c> wrapper. It cannot be one any more: hosting redirects a
/// hosted workflow's checkpoints only when the workflow agent is registered directly, so the per-call pieces
/// live on the chat client and the registered agent stays a plain workflow agent.
/// </remarks>
public sealed class CatalogueChatClientTests
{
    [Fact] // nothing in this client rewrites a message any more, and that is what removing the carrier bought
    public async Task Every_message_reaches_the_model_exactly_as_it_was_handed_in()
    {
        const string TheApplicationsRequest =
            """{ "runId": "run-1", "query": "a quiet corner", "currency": "IDR", "ceilingMonthly": null, "slots": [] }""";
        const string AnEarlierStagesAnswer = """{ "original_query": "a quiet corner", "categories": {} }""";

        var inner = new ScriptedChatClient("{}");

        await Build(inner, new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance), readsCatalogue: false)
            .GetResponseAsync(
            [
                new ChatMessage(ChatRole.User, TheApplicationsRequest),
                new ChatMessage(ChatRole.User, AnEarlierStagesAnswer),
            ]);

        // The request used to have a token lifted out of it and be re-serialized, and everything that was not a
        // request had to be recognised and left alone. Both rules are gone with the carrier: there is nothing to
        // take out, so there is nothing to recognise.
        inner.Requests.Should().ContainSingle()
            .Which.Should().Contain(TheApplicationsRequest).And.Contain(AnEarlierStagesAnswer);
    }

    [Fact] // a run with no token is offered nothing, so it cannot reach the catalogue at all
    public async Task A_catalogue_this_call_has_no_token_for_offers_no_tools()
    {
        var inner = new ScriptedChatClient("{}");

        // The endpoint is configured and this stage does read the catalogue: the only thing missing is the
        // caller's token, and that alone has to be enough to withhold the tools.
        await Build(
                inner,
                new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance),
                readsCatalogue: true,
                endpoint: "https://catalogue.example/mcp")
            .GetResponseAsync([new ChatMessage(ChatRole.User, Request("no token here"))]);

        inner.Options.Should().ContainSingle();
        inner.Options[0]!.Tools.Should().BeNullOrEmpty(
            "a run without the caller's authority must not be offered the catalogue, and must not discover it "
            + "by being refused at the far end");
    }

    [Fact]
    public async Task An_unconfigured_catalogue_offers_no_tools()
    {
        var inner = new ScriptedChatClient("{}");

        await Build(inner, new McpAccessTokenService(NullLogger<McpAccessTokenService>.Instance), readsCatalogue: true, endpoint: string.Empty)
            .GetResponseAsync([new ChatMessage(ChatRole.User, Request("a-callers-token"))]);

        inner.Options.Should().ContainSingle();
        inner.Options[0]!.Tools.Should().BeNullOrEmpty("no endpoint means no catalogue to offer");
    }

    private static AuthorisedMcpChatClient Build(
        ScriptedChatClient inner,
        McpAccessTokenService tokens,
        bool readsCatalogue,
        string endpoint = "")
    {
        var mcpSettings = new McpSetting(endpoint);
        var catalogue = new McpAuthorizationConnection(
            mcpSettings,
            tokens,
            NullLoggerFactory.Instance,
            NullLogger<McpAuthorizationConnection>.Instance);

        return new(
            inner,
            new StubRunScope(
                (typeof(IMcpAccessTokenService), tokens),
                (typeof(IMcpAuthorizationConnection), catalogue),
                (typeof(IToolGuardPipeline), TestGuardrails.Pipeline),
                (typeof(AccessTokenHeaderReader), TheInvocationARunArrivesIn.CarryingNothing())),
            readsCatalogue,
            TestGuardrails.AllowList,
            NullLogger<AuthorisedMcpChatClient>.Instance);
    }

    private static string Request(string token)
        => $$"""
            { "runId": "run-1", "query": "a quiet corner", "currency": "IDR", "ceilingMonthly": null,
              "slots": [], "mcpAccessToken": "{{token}}" }
            """;
}
