using System.Net;
using AgentFoundry.WorkspaceSuggestions.Catalogue;
using AwesomeAssertions;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// Reading the catalogue: one request, the compact projection, and no composing from half of it.
/// </summary>
public sealed class HttpCatalogueReaderTests
{
    private static readonly Uri Endpoint = new("https://catalogue.example/api/catalog");

    [Fact]
    public async Task The_catalogue_is_read_once_in_the_compact_projection()
    {
        var handler = new ScriptedCatalogueHandler(HttpStatusCode.OK, Page());
        var page = await Reader(handler).ReadAsync();

        page.Value.Should().HaveCount(2);
        page.Count.Should().Be(2);
        page.Total.Should().Be(2);
        page.Currency.Should().Be("IDR");

        handler.Calls.Should().Be(1);
        handler.Requested!.Query.Should().Be("?view=compact");
    }

    /// <summary>The token is asked for on each read, and carried as a bearer.</summary>
    /// <remarks>
    /// Asked for rather than held, because an access token expires and a reader that captured one at
    /// start-up would stop working hours later for a reason nothing connects to the token.
    /// </remarks>
    [Fact]
    public async Task Every_read_carries_a_fresh_bearer()
    {
        var token = new CountingToken();
        var handler = new ScriptedCatalogueHandler(HttpStatusCode.OK, Page());

        var reader = new HttpCatalogueReader(new HttpClient(handler), Endpoint, token);

        await reader.ReadAsync();
        await reader.ReadAsync();

        token.Asked.Should().Be(2);

        // Both reads, in order: a reader that held the first token would send it twice.
        handler.Authorizations.Should().Equal("Bearer token-1", "Bearer token-2");
    }

    /// <summary>An item's metadata is read, including the members this module does not model.</summary>
    /// <remarks>
    /// The application publishes four metadata members and this module models two. Extra members are not
    /// an error - and <c>bestFor</c> and <c>notFor</c> are read case-insensitively by nobody, which is the
    /// point: the shape is shared, so there is no second copy of it here to drift.
    /// </remarks>
    [Fact]
    public async Task An_item_arrives_with_its_metadata()
    {
        var page = await Reader(new ScriptedCatalogueHandler(HttpStatusCode.OK, Page())).ReadAsync();

        var desk = page.Value[0];

        desk.Sku.Should().Be("DSKWWZEB3USL");
        desk.Category.Should().Be("desk");
        desk.PricePerMonth.Should().Be(600000m);
        desk.Metadata.Tags.Should().Contain("standing");
        desk.Metadata.Attributes.Should().ContainKey("type").WhoseValue.Should().Be("sit-stand");
    }

    /// <summary>An endpoint that refuses is refused, rather than composed from.</summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task An_endpoint_that_refuses_is_not_composed_from(HttpStatusCode status)
    {
        var read = async () => await Reader(new ScriptedCatalogueHandler(status, "{}")).ReadAsync();

        await read.Should().ThrowAsync<IncompleteCatalogueException>();
    }

    /// <summary>
    /// A truncated page is refused, because a partial catalogue reads exactly like a complete one.
    /// </summary>
    /// <remarks>
    /// This is what the application's own <c>total</c> is for. Without the check a run would compose from
    /// whatever arrived, and a slot the page happened to be missing from would look like a slot the
    /// catalogue cannot fill - wrong, and confidently wrong.
    /// </remarks>
    [Fact]
    public async Task A_truncated_catalogue_is_refused()
    {
        var truncated = Page(count: 2, total: 140);

        var read = async () => await Reader(new ScriptedCatalogueHandler(HttpStatusCode.OK, truncated)).ReadAsync();

        await read.Should().ThrowAsync<IncompleteCatalogueException>().WithMessage("*2 of 140*");
    }

    [Fact]
    public async Task A_body_that_is_not_a_page_is_refused()
    {
        var read = async () => await Reader(new ScriptedCatalogueHandler(HttpStatusCode.OK, "not json")).ReadAsync();

        await read.Should().ThrowAsync<Exception>();
    }

    private static HttpCatalogueReader Reader(HttpMessageHandler handler)
        => new(new HttpClient(handler), Endpoint, new CountingToken());

    private static string Page(int count = 2, int total = 2)
        => $$"""
        {
          "value": [
            {
              "sku": "DSKWWZEB3USL",
              "name": "A standing desk",
              "category": "desk",
              "subCategory": null,
              "pricePerMonth": 600000,
              "description": "A desk you can stand at.",
              "metadata": {
                "tags": ["standing"],
                "attributes": { "type": "sit-stand" },
                "bestFor": ["a bad back"],
                "notFor": []
              }
            },
            {
              "sku": "CHAE2V0VGJZ8",
              "name": "A chair",
              "category": "chair",
              "subCategory": null,
              "pricePerMonth": 400000,
              "description": "A chair.",
              "metadata": { "tags": [], "attributes": {}, "bestFor": [], "notFor": [] }
            }
          ],
          "count": {{count}},
          "total": {{total}},
          "truncated": {{(count < total).ToString().ToLowerInvariant()}},
          "currency": "IDR"
        }
        """;

    /// <summary>A token that says which time it was asked, so a fresh one is visible.</summary>
    private sealed class CountingToken : ICatalogueAccessToken
    {
        public int Asked { get; private set; }

        public ValueTask<string> TokenAsync(CancellationToken cancellationToken = default)
        {
            Asked++;

            return ValueTask.FromResult($"token-{Asked}");
        }
    }
}
