using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using CoreRentalNet.Host.Infrastructure;
using Xunit;

namespace CoreRentalNet.Host.Tests;

/// <summary>
/// The compact projection of the catalogue: the fields a machine caller reads, and the fields it
/// deliberately does not.
/// </summary>
/// <remarks>
/// The caller this exists for names SKUs and recomputes amounts, so the image path and the display
/// flags are weight it cannot use - measured at 34% of the answer before the projection existed. What
/// it does read, the description and the metadata, must survive.
/// </remarks>
public sealed class CatalogApiCompactViewTests(CatalogApiFactory factory) : IClassFixture<CatalogApiFactory>
{
    [Fact] // API-32
    public async Task The_compact_answer_carries_what_a_caller_reads_and_not_what_it_cannot()
    {
        var item = await FirstItemAsync("/api/catalog?view=compact&subCategory=monitor");

        item.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["sku", "name", "category", "subCategory", "pricePerMonth", "description", "metadata"]);

        // The three fields the projection exists to drop, and the shape the price takes: a number,
        // because the currency is stated once on the envelope rather than on every row.
        item.TryGetProperty("imagePath", out _).Should().BeFalse();
        item.TryGetProperty("imageAvailable", out _).Should().BeFalse();
        item.TryGetProperty("isFeatured", out _).Should().BeFalse();
        item.GetProperty("pricePerMonth").ValueKind.Should().Be(JsonValueKind.Number);
        item.GetProperty("metadata").GetProperty("tags").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact] // API-35
    public async Task The_full_answer_still_carries_what_the_compact_one_leaves_out()
    {
        // Non-vacuity for API-32: without this, a projection that had quietly become a copy of the
        // full view would still satisfy every "does not carry" assertion above by carrying nothing.
        var full = await FirstItemAsync("/api/catalog?subCategory=monitor");

        full.TryGetProperty("imagePath", out var imagePath).Should().BeTrue();
        imagePath.GetString().Should().NotBeNullOrWhiteSpace();
        full.TryGetProperty("isFeatured", out _).Should().BeTrue();
        full.GetProperty("monthlyPrice").ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact] // API-33
    public async Task The_currency_is_stated_once_on_the_envelope_and_never_on_a_row()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog?view=compact");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = document.RootElement;

        body.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            ["value", "count", "currency"]);

        // The catalogue is priced in one currency and the loader refuses a row that is not, so
        // stating it here is truthful - and it is what makes the price a number.
        body.GetProperty("currency").GetString().Should().Be("IDR");
        body.GetProperty("value").GetArrayLength().Should().Be(140);
        body.GetProperty("count").GetInt32().Should().Be(140);
    }

    [Fact] // API-33
    public async Task An_empty_answer_still_states_the_currency_it_is_priced_in()
    {
        // The envelope's currency comes from the catalogue, not from the rows that matched: a filter
        // that matches nothing must not leave the answer unable to say what its prices would have
        // been in.
        var response = await factory.CreateClient().GetAsync("/api/catalog?view=compact&search=zzzzzzzz");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        document.RootElement.GetProperty("count").GetInt32().Should().Be(0);
        document.RootElement.GetProperty("currency").GetString().Should().Be("IDR");
    }

    [Fact] // API-34
    public async Task An_unknown_view_is_a_400_naming_the_allowed_values()
    {
        var response = await factory.CreateClient().GetAsync("/api/catalog?view=wide");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var body = problem.RootElement;

        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("code").GetString().Should().Be(ApiErrorCode.UnknownFilter);

        // Refused with the words that would have worked, the same answer an unknown filter gets.
        var refusal = body.GetProperty("errors").GetProperty("view")[0].GetString()!;
        refusal.Should().Contain("wide").And.Contain("full").And.Contain("compact");
    }

    [Fact] // API-32
    public async Task Saying_nothing_returns_the_full_view()
    {
        // The default is the published shape, so a caller that does not know about the projection sees
        // exactly what it saw before it existed.
        var item = await FirstItemAsync("/api/catalog?subCategory=monitor");

        item.TryGetProperty("imagePath", out _).Should().BeTrue();
        item.TryGetProperty("metadata", out _).Should().BeTrue();
    }

    private async Task<JsonElement> FirstItemAsync(string url)
    {
        var response = await factory.CreateClient().GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("value")[0].Clone();
    }
}
