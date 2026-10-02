using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Configs;
using CoreRentalNet.Host.Infrastructure;
using Xunit;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>A run whose own call was cancelled: the stream ends, and the run is still recorded.</summary>
/// <remarks>
/// The endpoint swallows this cancellation rather than writing a failure frame or aborting the response, because
/// a customer who stopped and a transport that gave up look the same from here - and neither is a failure the
/// customer could act on.
/// </remarks>
public sealed class SuggestionCancellationTests
{
    private const string Permission = "builder:ai";

    [Fact] // the stream ends rather than the connection being aborted with the run half written
    public async Task A_cancelled_run_ends_the_stream_without_a_failure_frame()
    {
        using var factory = new SuggestionEndpointFactory(new CancellingSuggestionAgentAdapter());
        using var response = await PostAsync(factory);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var frames = await response.Content.ReadAsStringAsync();

        frames.Should().Contain("event: stage", "the frame written before the cancellation reached the customer");
        frames.Should().NotContain("event: failed", "a cancelled run is an ending, not a failure");
        frames.Should().NotContain("event: result");
    }

    [Fact] // and the run it cancelled is still one the application can explain afterwards
    public async Task A_cancelled_run_is_still_recorded()
    {
        using var factory = new SuggestionEndpointFactory(new CancellingSuggestionAgentAdapter());

        factory.Logs.Clear();

        using var response = await PostAsync(factory);

        var record = factory.Logs.Entries
            .Where(entry => entry.Category == LoggerWorkspaceSuggestionRunRecordWriter.Category)
            .Should().ContainSingle().Subject;

        record.Message.Should().Contain("\"verdict\":\"unavailable\"");
    }

    private static async Task<HttpResponseMessage> PostAsync(SuggestionEndpointFactory factory)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, BuilderRoutes.Suggest)
        {
            Content = JsonContent.Create(new { query = "a desk and a chair" }),
        };

        request.Headers.Add(CatalogApiTestHandler.PermissionsHeader, Permission);

        return await factory.CreateClient().SendAsync(request);
    }
}
