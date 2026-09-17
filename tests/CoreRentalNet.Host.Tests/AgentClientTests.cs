using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using CoreRentalNet.Host.Agents;
using CoreRentalNet.Host.Composition;
using CoreRentalNet.Modules.Workspace.Application.Contracts.Suggestion;
using CoreRentalNet.E2E.LocalAgent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The application's agent client against a stand-in that speaks the protocol.
/// </summary>
/// <remarks>
/// <para>
/// The adapter's own tests prove it reads a stream it was handed. These prove the thing that produces
/// that stream is read correctly - that a real client, built the way the application builds it, can
/// follow the protocol's events and hand back the text. Without them the parsing could be perfect and
/// the client still pointed at the wrong event name.
/// </para>
/// <para>
/// The stand-in runs in this process on a port this test chose. Nothing here reaches a network, holds a
/// credential or reads a file, so it runs wherever the suite runs.
/// </para>
/// </remarks>
public sealed class AgentClientTests : IAsyncLifetime
{
    private WebApplication standIn = null!;

    private string endpoint = null!;

    public async Task InitializeAsync()
    {
        var port = FreePort();
        endpoint = $"http://127.0.0.1:{port}";

        var builder = LocalAgentApp.Create([]);

        builder.WebHost.UseUrls(endpoint);

        standIn = builder.Build();

        LocalAgentEndpoints.Map(standIn);

        await standIn.StartAsync();
    }

    public async Task DisposeAsync() => await standIn.DisposeAsync();

    /// <summary>
    /// A configured agent, built through the application's own registration.
    /// </summary>
    /// <remarks>
    /// Through the registration rather than around it, because the thing under test is the wiring: a
    /// test that built its own client would pass while the application's was pointed somewhere else.
    /// </remarks>
    private IAgentSuggestions Agent()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Agent:Endpoint"] = endpoint,
            ["Agent:Name"] = "stand-in",
        });

        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.AddFoundryAgent();

        return builder.Build().Services.GetRequiredService<IAgentSuggestions>();
    }

    [Fact]
    public async Task A_configured_agent_reads_the_run_the_stand_in_streams()
    {
        var agent = Agent();

        Assert.True(agent.IsConfigured);

        var messages = new List<AgentSuggestionMessage>();

        await foreach (var message in agent.AskAsync("a desk and a chair and a monitor"))
        {
            messages.Add(message);
        }

        // Four stages, in order, then the result.
        Assert.Equal(
            ["verifying", "rephrasing", "selecting", "reviewing"],
            messages.Where(message => message.Kind == "stage").Select(message => message.Stage!));

        var result = Assert.Single(messages, message => message.Kind == "result").Result!;

        Assert.Equal(SuggestionStatus.Ok, result.Status);
        Assert.Equal(3, result.Options.Count);
        Assert.Equal(["low", "middle", "high"], result.Options.Select(option => option.Tier));
    }

    /// <summary>
    /// The result survives the protocol's framing: every line of a three-option answer arrives whole.
    /// </summary>
    /// <remarks>
    /// The result is the longest thing the agent sends, so it is the thing most likely to be split across
    /// events. This asserts on the whole of it - three options, three lines each, and the SKU in each -
    /// because a truncated read would still produce a result, just a smaller one.
    /// </remarks>
    [Fact]
    public async Task The_whole_result_arrives_across_the_streamed_events()
    {
        var agent = Agent();

        AgentSuggestion? result = null;

        await foreach (var message in agent.AskAsync("a desk, a chair and a monitor"))
        {
            if (message.Kind == "result")
            {
                result = message.Result;
            }
        }

        Assert.NotNull(result);
        Assert.Equal(3, result.Options.Count);

        foreach (var option in result.Options)
        {
            Assert.Equal(3, option.Lines.Count);
            Assert.All(option.Lines, line => Assert.False(string.IsNullOrWhiteSpace(line.Sku)));
        }
    }

    /// <summary>A run the agent could not settle arrives as itself, not as a failure to read it.</summary>
    /// <remarks>
    /// Only two scenarios, because the agent's contract has three statuses and all of them are answers.
    /// <c>unavailable</c> is not among them: it belongs to the application, and it is reached by the agent
    /// failing to answer rather than by the agent answering badly.
    /// </remarks>
    [Theory]
    [InlineData("exhausted", SuggestionStatus.Exhausted)]
    [InlineData("rejected", SuggestionStatus.Rejected)]
    public async Task An_outcome_other_than_ok_is_read_as_that_outcome(string scenario, string status)
    {
        await Choose(scenario);

        AgentSuggestion? result = null;

        await foreach (var message in Agent().AskAsync("a desk"))
        {
            result = message.Result ?? result;
        }

        Assert.NotNull(result);
        Assert.Equal(status, result.Status);

        // A refusal carries its code and nothing else; an exhaustion carries neither a code nor nothing
        // at all - it carries the candidates and what was wrong with them. The two are different shapes
        // on purpose, and asserting one shape for both is how that distinction would be lost.
        if (status == SuggestionStatus.Rejected)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.Code));
            Assert.Empty(result.Options);
        }
        else
        {
            Assert.Null(result.Code);
            Assert.NotEmpty(result.Options);
            Assert.NotEmpty(result.Findings);
        }
    }

    /// <summary>
    /// A line the contract does not allow costs a line, and the run still finishes.
    /// </summary>
    /// <remarks>
    /// The stand-in also names a stage outside the vocabulary, and it is passed through rather than
    /// dropped here: the closed list belongs to the agent's schema, and the application's answer to a
    /// stage it has no words for is to render nothing - which is a rendering decision with its own test,
    /// not something the transport should be quietly deciding.
    /// </remarks>
    [Fact]
    public async Task A_line_the_contract_does_not_allow_is_skipped_rather_than_ended_on()
    {
        await Choose("malformed");

        var messages = new List<AgentSuggestionMessage>();

        await foreach (var message in Agent().AskAsync("a desk"))
        {
            messages.Add(message);
        }

        // "not json at all" is gone; everything the adapter could read is here, in order.
        Assert.Equal(
            ["verifying", "a-stage-nobody-defined"],
            messages.Where(message => message.Kind == "stage").Select(message => message.Stage!));

        Assert.NotNull(Assert.Single(messages, message => message.Kind == "result").Result);
    }

    /// <summary>
    /// A deployment with no agent configured still has one, and it says so.
    /// </summary>
    /// <remarks>
    /// The null object rather than a missing registration. This is the difference between a page that
    /// offers a suggestion and is told the service is unavailable, and an application that does not
    /// start - and it is the state a developer's own machine is in.
    /// </remarks>
    [Fact]
    public void No_agent_configured_is_a_state_rather_than_a_missing_service()
    {
        var builder = WebApplication.CreateBuilder();

        builder.AddFoundryAgent();

        var agent = builder.Build().Services.GetRequiredService<IAgentSuggestions>();

        Assert.False(agent.IsConfigured);
    }

    /// <summary>
    /// An agent that fails becomes the one failure the application layer understands.
    /// </summary>
    /// <remarks>
    /// The SDK reports a refused connection, a rejected call and a broken stream as its own exception
    /// types, and the module must not know them. Everything that is not a cancellation leaves as
    /// <see cref="HttpRequestException"/>, so "the agent could not be reached" has one meaning on the
    /// inside however many ways the client finds to say it - and a status the contract does not have can
    /// never be mistaken for an answer.
    /// </remarks>
    [Fact]
    public async Task An_agent_that_fails_is_reported_as_one_kind_of_failure()
    {
        await Choose("broken");

        var agent = Agent();

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in agent.AskAsync("a desk"))
            {
                // Nothing to read: the stand-in refuses the call outright.
            }
        });
    }

    /// <summary>A cancellation is the customer's, and passes through as itself.</summary>
    [Fact]
    public async Task A_cancelled_run_is_not_reported_as_an_unreachable_agent()
    {
        await Choose("slow");

        using var stop = new CancellationTokenSource();

        var agent = Agent();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in agent.AskAsync("a desk", stop.Token))
            {
                await stop.CancelAsync();
            }
        });
    }

    private async Task Choose(string scenario)
    {
        using var client = new HttpClient();

        var response = await client
            .PostAsJsonAsync($"{endpoint}/scenario", new ScenarioRequest(scenario))
            ;

        response.EnsureSuccessStatusCode();
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);

        listener.Start();

        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();

        return port;
    }
}
