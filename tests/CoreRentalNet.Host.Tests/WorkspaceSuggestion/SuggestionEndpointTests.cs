using System.Net;
using System.Net.Http.Json;
using System.Text;
using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using CoreRentalNet.Host.Presentation.WorkspaceSuggestion;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Run;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>
/// The run endpoint over a real request: its route, the body it binds, the gate in front of it and each way it
/// refuses.
/// </summary>
/// <remarks>
/// In process, because what is asserted here is the boundary the controller declares, and because a unit test
/// cannot see the route, the binding, the status codes or the framing. The browser tier covers the same endpoint
/// through the page, but it starts a stand-in agent this repository does not currently build - so this is the
/// tier that keeps the endpoint honest until that is dealt with.
/// </remarks>
public sealed class SuggestionEndpointTests(SuggestionEndpointFactory factory, ITestOutputHelper output)
    : IClassFixture<SuggestionEndpointFactory>
{
    private const string Permission = "builder:ai";

    /// <summary>A permission the catalogue hands out, and not the one a run costs.</summary>
    private const string OtherPermission = "read:catalog";

    [Fact] // the route, the gate, the binding and the stream, end to end
    public async Task A_signed_in_customer_with_the_permission_is_answered_with_the_stream()
    {
        var response = await PostAsync(Permission, new { query = "a desk and a chair, for a small room" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be(ServerSentWorkspaceSuggestionEventWriter.MediaType);

        var frames = await response.Content.ReadAsStringAsync();

        output.WriteLine(frames);

        // The stages are the agent's to announce, and no agent is configured in this environment, so the run
        // reports itself unavailable with nothing before it - which is the outcome that proves the run reached
        // the agent at all rather than answering from memory.
        frames.Should().Contain("event: failed");
        frames.Should().Contain(WorkspaceSuggestionFailureCode.Unavailable);
    }

    [Fact] // the record is written whichever way the run ended
    public async Task A_run_writes_one_record_line()
    {
        factory.Logs.Clear();

        var response = await PostAsync(Permission, new { query = "a desk and a chair" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // One line, and it carries the run: the record is what makes a paid call explainable afterwards, and a
        // run that left none is exactly the run nobody can explain.
        var record = factory.Logs.Entries
            .Where(entry => entry.Category == LoggerWorkspaceSuggestionRunRecordWriter.Category)
            .Should().ContainSingle().Subject;

        record.Message.Should().Contain("Suggestion run");
        record.Message.Should().Contain("\"verdict\":\"unavailable\"");

        // And the account is on it as a hash rather than as an account: a 64-character SHA-256 digest, which is
        // precise rather than "some string", so this cannot pass by the field being empty.
        record.Message.Should().MatchRegex("\"customerId\":\"[0-9A-F]{64}\"");
    }

    [Fact] // hiding the panel is presentation; this is the authorisation
    public async Task A_signed_in_customer_without_the_permission_is_refused()
    {
        var response = await PostAsync(OtherPermission, new { query = "a desk and a chair" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_sentence_of_nothing_but_spaces_is_refused_before_anything_is_spent()
    {
        var response = await PostAsync(Permission, new { query = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain(ApiErrorCode.InvalidRequest);
        body.Should().NotContain("event:", "nothing was streamed, so the refusal is the whole answer");
    }

    [Fact] // the body the framework binds to nothing
    public async Task A_request_that_posts_null_is_refused_rather_than_run()
    {
        var response = await PostAsync(Permission, body: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>Posts a run as the named permissions, with <c>null</c> posted as the JSON literal it is.</summary>
    [Fact] // a session that cannot produce a token is refused before the run is paid for
    public async Task A_session_that_cannot_produce_a_token_is_refused_before_the_run_starts()
    {
        factory.Logs.Clear();

        var response = await PostAsync(Permission, new { query = "a desk and a chair" }, withAccessToken: false);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var problem = await response.Content.ReadAsStringAsync();

        problem.Should().Contain(
            "session has expired",
            "the refusal names the session rather than the customer's mistake");
        factory.Logs.Entries.Should().NotContain(
            entry => entry.Category == LoggerWorkspaceSuggestionRunRecordWriter.Category,
            "nothing was streamed, so no run was started or recorded");
    }

    private async Task<HttpResponseMessage> PostAsync(string permission, object? body, bool withAccessToken = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuilderRoutes.Suggest)
        {
            Content = body is null
                ? new StringContent("null", Encoding.UTF8, "application/json")
                : JsonContent.Create(body),
        };

        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, permission);

        if (!withAccessToken)
        {
            request.Headers.Add(CatalogApiTestHandler.WithoutAccessTokenHeader, "true");
        }

        return await factory.CreateClient().SendAsync(request);
    }
}
