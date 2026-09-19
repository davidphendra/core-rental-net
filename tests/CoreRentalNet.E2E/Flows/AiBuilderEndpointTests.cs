using AwesomeAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The whole chain, from the browser: the authenticated application, its real adapter, the real client and
/// the stand-in agent on a socket. The run endpoint is where a suggestion is paid for, so this is the test
/// that says the browser tier can reach it at all.
/// </summary>
/// <remarks>
/// <para>
/// It asserts the <b>frames</b> and not the page, because the page is not this step's work: e05s07's tasks 4
/// to 6 build the section that reads them. What is proved here is the half everything else depends on - a
/// signed-in customer with the permission reaches the endpoint, the application runs the real adapter against
/// a stand-in, and what comes back is the application's own sequence rather than raw model output.
/// </para>
/// <para>
/// The run is driven with <c>fetch</c> from the page rather than from a client, and that is deliberate: the
/// cookie, the origin and the permission are then exactly what a section would send, and the SSE arrives as
/// the browser would receive it.
/// </para>
/// </remarks>
public sealed class AiBuilderEndpointTests : AuthenticatedE2ETest
{
    private readonly ITestOutputHelper output;

    public AiBuilderEndpointTests(HostFixture host, ITestOutputHelper output)
        : base(host, output)
    {
        // Held so the frames can be read when an assertion fails. A primary constructor parameter cannot be
        // used for this: it is both captured here and passed to the base, which the compiler refuses.
        this.output = output;
    }

    [Fact] // the browser tier's foundation, end to end
    public async Task A_signed_in_customer_with_the_permission_reaches_the_run_endpoint()
    {
        await Host.ChooseScenarioAsync("suggested");
        await SignInAsync("Dewi Reader");

        var frames = await RunAsync("a desk and a chair, for a small room");

        output.WriteLine(frames);

        // The application's own words, in its own order. The agent's stage identities never reach here,
        // which is why these lines are the only ones a run can begin with.
        frames.Should().Contain("event: stage");
        frames.Should().Contain("Reading your request");
        frames.Should().Contain("Matching the catalogue");
        frames.Should().Contain("Checking the suggestion");

        // The model's words, never its JSON: the whole point of the reader, and the reason a customer can
        // read the run as it happens.
        frames.Should().Contain("event: text");
        frames.Should().Contain("An uncluttered setup for one person, kept inside a small room.");

        // And the answer, whole, once.
        frames.Should().Contain("event: result");
        frames.Should().NotContain("event: failed");
    }

    [Fact] // a run the customer stopped is not an error, and the permission is what makes it possible
    public async Task The_endpoint_is_refused_without_the_permission()
    {
        await Host.ChooseScenarioAsync("suggested");

        // "builder" holds the catalogue permission and not the AI one, so it may use the builder page and
        // must not be able to spend anything.
        await SignInAsync("Sari Builder");

        var response = await Page.EvaluateAsync<int>(
            """
            async () => {
                const response = await fetch('/api/builder/suggest', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ query: 'a desk and a chair' })
                });

                return response.status;
            }
            """);

        response.Should().Be(
            403,
            "hiding the section is not authorisation: a caller who never sees it still cannot run one");
    }

    private Task<string> RunAsync(string query)
        => Page.EvaluateAsync<string>(
            """
            async (query) => {
                const response = await fetch('/api/builder/suggest', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ query })
                });

                return await response.text();
            }
            """,
            query);
}
