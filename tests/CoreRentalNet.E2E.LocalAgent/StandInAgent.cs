namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>The stand-in's entry point, named rather than generated.</summary>
/// <remarks>
/// Top-level statements would generate a <c>Program</c> class in the global namespace, and this project is
/// compiled into the test assembly that also holds the application's <c>Program</c>: two types of the same
/// name there make every reference to either of them ambiguous. A named entry point costs one method and
/// removes the collision.
/// </remarks>
internal static class StandInAgent
{
    public static async Task Main(string[] args)
    {
        var app = LocalAgentApp.Create(args).Build();

        LocalAgentEndpoints.Map(app);

        await app.RunAsync().ConfigureAwait(false);
    }
}
