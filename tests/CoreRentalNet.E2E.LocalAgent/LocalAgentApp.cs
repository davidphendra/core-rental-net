namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>The stand-in as an application, so the suite can start it as a process and a test can start it
/// in its own process.</summary>
/// <remarks>
/// A function rather than configuration inside an entry point, because the tests that hold the application's
/// client against this stand-in want it on a free port they chose and in-process, with no process to wait for
/// and reap. The browser suite wants the opposite, and both are given the same agent.
/// </remarks>
internal static class LocalAgentApp
{
    /// <param name="args">The args a process was started with; empty when a test starts it in-process.</param>
    /// <param name="onRequest">
    /// Called with the body of every answer request, for the test that holds the client's own request shape -
    /// the structured-output format - against the wire. Null for a process, which has nobody to tell.
    /// </param>
    public static WebApplicationBuilder Create(string[] args, Action<string>? onRequest = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<ScenarioSelection>();
        builder.Services.AddSingleton(new RequestObserver(onRequest));

        // A stand-in's own noise is not what a failing test is about.
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        return builder;
    }
}
