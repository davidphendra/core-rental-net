using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoreRentalNet.CatalogPopulate.Metadata;

/// <summary>Calls a local OpenAI-compatible server for one product's generated metadata.</summary>
/// <remarks>
/// Temperature is sent as zero rather than left to the server, because the catalogue is expected to be
/// reproducible for a pinned model and prompt. The request body is the chat-completions shape the
/// llama.cpp server already exposes for the embedding tool.
/// </remarks>
internal sealed class OpenAiCompatibleMetadataGenerator(HttpClient client, string model) : IProductMetadataGenerator
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public async Task<GeneratedProductMetadata> GenerateAsync(
        MetadataGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var body = Request(request);
        using var response = await client.PostAsJsonAsync("chat/completions", body, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var content = Content(json);

        return JsonSerializer.Deserialize<GeneratedProductMetadata>(content, s_options)
            ?? throw new InvalidOperationException("The model's answer was not the generated-metadata contract.");
    }

    private object Request(MetadataGenerationRequest request)
    {
        var product = request.Product;

        return new
        {
            model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = PromptCatalog.Text },
                new { role = "user", content = JsonSerializer.Serialize(new
                {
                    product = new
                    {
                        title = product.Meta.Title,
                        description = string.Join(" ", product.Meta.Description),
                        categories = string.Join(" > ", product.Meta.Categories),
                        priceUsd = product.Meta.Price,
                        rating = product.Meta.AverageRating,
                        ratingCount = product.Meta.RatingNumber,
                        store = product.Meta.Store,
                        details = product.Meta.Details.ToDictionary(pair => pair.Key, pair => pair.Value.ToString()),
                    },
                    reviews = request.Sample.Select(review => new
                    {
                        rating = review.Rating,
                        title = review.Title,
                        text = review.Text,
                        helpfulVotes = review.HelpfulVote,
                    }),
                    reviewCount = product.ReviewCount,
                }) },
            },
        };
    }

    private static string Content(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            ?? throw new InvalidOperationException("The model returned no message content.");
    }
}
