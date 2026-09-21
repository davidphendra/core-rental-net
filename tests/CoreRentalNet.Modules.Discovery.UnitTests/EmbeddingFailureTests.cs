using AwesomeAssertions;
using CoreRentalNet.BuildingBlocks.Infrastructure.Sqlite;
using CoreRentalNet.Modules.Discovery.Application.Shortlist;
using CoreRentalNet.Modules.Discovery.Infrastructure;
using CoreRentalNet.Modules.Discovery.Infrastructure.Shortlist;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreRentalNet.Modules.Discovery.UnitTests;

/// <summary>
/// What happens when the deployment cannot embed the request.
/// </summary>
/// <remarks>
/// The outage and the defect are proved to be different things here. A caller that reported <b>every</b>
/// exception as "unavailable" would turn a bug in this application into a support ticket that says "try again
/// later" for as long as nobody looked — so the type that means an outage is asserted, and so is the fact that
/// cancellation is not one.
/// </remarks>
public sealed class EmbeddingFailureTests
{
    /// <summary>A context that is never queried: the failure happens before anything is read.</summary>
    private static DiscoveryContext UnqueriedContext()
    {
        var options = new DbContextOptionsBuilder<DiscoveryContext>();
        DiscoveryPersistence.Configure(options, new SqliteDatabaseSettings("never-opened.db"));

        return new DiscoveryContext(options.Options);
    }

    private static CatalogShortlist ShortlistOver(StubEmbeddingClient embeddings)
        => new(embeddings, new FakeProductCatalog(), UnqueriedContext(), ShortlistSettings.Default);

    [Fact] // SCR-12
    public async Task A_deployment_that_refuses_the_call_is_unavailable()
    {
        var embeddings = new StubEmbeddingClient(_ => throw new HttpRequestException("the deployment is unreachable"));

        var act = () => ShortlistOver(embeddings).ForAsync("a quiet corner", CancellationToken.None);

        await act.Should().ThrowAsync<ShortlistUnavailableException>().WithMessage("*did not answer*");
    }

    [Fact] // SCR-12
    public async Task A_deployment_that_answers_with_several_vectors_is_unavailable_rather_than_guessed()
    {
        // One text in, three vectors out. Picking one would be a guess about which vector describes the
        // request, and the shortlist built from the wrong one would look entirely healthy.
        var embeddings = new StubEmbeddingClient(_ => [[1f, 0f], [0f, 1f], [1f, 1f]]);

        var act = () => ShortlistOver(embeddings).ForAsync("a quiet corner", CancellationToken.None);

        await act.Should().ThrowAsync<ShortlistUnavailableException>().WithMessage("*3 vectors*");
    }

    [Fact] // SCR-12
    public async Task A_deployment_that_answers_with_no_vector_at_all_is_unavailable()
    {
        var embeddings = new StubEmbeddingClient(_ => []);

        var act = () => ShortlistOver(embeddings).ForAsync("a quiet corner", CancellationToken.None);

        await act.Should().ThrowAsync<ShortlistUnavailableException>();
    }

    [Fact] // SCR-12
    public async Task A_cancelled_run_is_not_reported_as_an_outage()
    {
        // The customer stopped the run, or navigated away. That is a neutral ending the application models and
        // reports separately; dressing it up as an outage would put a failure in the record for something
        // somebody chose to do.
        var embeddings = new StubEmbeddingClient(_ => [[1f, 0f]]);

        var act = () => ShortlistOver(embeddings).ForAsync("a quiet corner", new CancellationToken(canceled: true));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact] // SCR-12
    public async Task The_request_is_embedded_once_and_the_failure_stops_there()
    {
        // The embedding happens before anything is read, so an outage costs no work and - more to the point -
        // a caller cannot be handed a shortlist assembled from a stale query vector, or from none at all.
        var embeddings = new StubEmbeddingClient(_ => throw new HttpRequestException("unreachable"));

        var act = () => ShortlistOver(embeddings).ForAsync("a quiet corner", CancellationToken.None);

        await act.Should().ThrowAsync<ShortlistUnavailableException>();
        embeddings.Calls.Should().ContainSingle().Which.Should().Equal("a quiet corner");
    }
}
