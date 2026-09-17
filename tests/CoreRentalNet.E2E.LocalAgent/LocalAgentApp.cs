namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>
/// The stand-in as an application, so that the suite can start it as a process and a test can start it
/// in its own process.
/// </summary>
/// <remarks>
/// A function rather than configuration inside an entry point, because the tests that exercise the
/// application's client against this stand-in want it on a free port they chose, in-process, with no
/// process to wait for and reap. The browser suite wants the opposite, and both are given the same agent.
/// </remarks>
internal static class LocalAgentApp
{
    public static WebApplicationBuilder Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<ScenarioSelection>();

        // A stand-in's own noise is not what a failing test is about.
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        return builder;
    }
}
