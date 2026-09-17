namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>What the stand-in serves, which is the whole of what it is.</summary>
internal static class LocalAgentEndpoints
{
    public static void Map(WebApplication app)
    {
        // Ready to be asked, and asked nothing else: the suite waits on this rather than on a route that
        // would run the agent and change the scenario while another test is reading it.
        app.MapGet("/health", () => Results.Ok(new { status = "ready" }));

        app.MapGet("/scenario", (ScenarioSelection selection) => Results.Ok(new
        {
            current = selection.Current,
            available = ScenarioLibrary.Names,
        }));

        // What the tests drive. A test sets the scenario it is about to exercise, so that a run's answer
        // is chosen by the test rather than guessed from the customer's words.
        app.MapPost("/scenario", (ScenarioRequest request, ScenarioSelection selection)
            => selection.Choose(request.Name)
                ? Results.Ok(new { current = selection.Current })
                : Results.NotFound(new { error = $"no scenario named '{request.Name}'" }));

        app.MapPost("/responses", AnswerAsync);
    }

    /// <summary>One response, streamed: the agent's contract as the protocol's events.</summary>
    private static async Task AnswerAsync(
        HttpContext context,
        ScenarioSelection selection,
        ILogger<ScenarioSelection> logger)
    {
        using var reader = new StreamReader(context.Request.Body);

        var body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);

        logger.LogWarning(
            "stand-in answering {Scenario} for a {Length} character request",
            selection.Current,
            body.Length);

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";

        await SseWriter.WriteAsync(
                context.Response,
                Model(body),
                ScenarioLibrary.For(selection.Current),
                ScenarioLibrary.DelayMilliseconds(selection.Current),
                context.RequestAborted)
            .ConfigureAwait(false);
    }

    /// <summary>The model the request named, echoed back.</summary>
    /// <remarks>
    /// Read from the request rather than fixed, because the client sets it and a response naming a
    /// different model than the one asked for is a response some clients refuse.
    /// </remarks>
    private static string Model(string body)
    {
        using var document = System.Text.Json.JsonDocument.Parse(body);

        return document.RootElement.TryGetProperty("model", out var model)
            && model.ValueKind == System.Text.Json.JsonValueKind.String
                ? model.GetString()!
                : "hosted-agent";
    }
}
