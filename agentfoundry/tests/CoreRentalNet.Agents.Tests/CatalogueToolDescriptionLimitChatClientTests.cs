using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Agents;
using CoreRentalNet.Agents.Features.WorkspaceSuggestion.Domain.Tools;
using CoreRentalNet.Agents.Shared.Guardrails.Abstractions;
using CoreRentalNet.Agents.Shared.Guardrails.Results;
using CoreRentalNet.Agents.Shared.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

/// <summary>The model-facing description is short; every other product property survives.</summary>
public sealed class CatalogueToolDescriptionLimitChatClientTests
{
    private const string LongDescription =
        "This 60\" wide desk shell is part of the HON Mod Desk Collection that delivers simplicity and " +
        "adaptability to enhance your productivity. The contemporary design feels perfectly at home in " +
        "either a private office or an open floor plan.";

    [Fact]
    public async Task A_long_description_is_limited_to_one_hundred_text_elements()
    {
        var result = await CallWithCatalogueResult(FullAnswer(LongDescription));

        var product = ProductOf(result);
        var description = product["description"]!.GetValue<string>();

        new StringInfo(description).LengthInTextElements.Should().Be(100);
        product["sku"]!.GetValue<string>().Should().Be("DSKB0B6MCK87");
        product["name"]!.GetValue<string>().Should().Contain("Slate Teak");
        product["imagePath"]!.GetValue<string>().Should().Be("/images/vendored/DSKB0B6MCK87.jpg");
        product["imageAvailable"]!.GetValue<bool>().Should().BeTrue();
        product["isFeatured"]!.GetValue<bool>().Should().BeFalse();
        product["monthlyPrice"]!["amount"]!.GetValue<decimal>().Should().Be(271000m);
    }

    [Fact]
    public async Task A_short_description_is_left_unchanged()
    {
        const string shortDescription = "A compact desk for a home office.";
        var result = await CallWithCatalogueResult(FullAnswer(shortDescription));

        ProductOf(result)["description"]!.GetValue<string>().Should().Be(shortDescription);
    }

    [Fact]
    public async Task A_json_element_result_is_compacted()
    {
        using var document = JsonDocument.Parse(FullAnswer(LongDescription));
        var result = await CallWithResult(
            CatalogueSearchToolNames.NameSearch,
            document.RootElement.Clone());

        var description = ProductOf(result)["description"]!.GetValue<string>();

        new StringInfo(description).LengthInTextElements.Should().Be(100);
    }

    [Fact]
    public async Task A_non_catalogue_tool_result_is_left_unchanged()
    {
        var original = FullAnswer(LongDescription);
        var result = await CallWithResult("another_tool", original);

        result.Should().Be(original);
    }

    [Fact]
    public async Task A_malformed_result_is_left_unchanged()
    {
        const string malformed = "{\"matches\":{\"value\":[";
        var result = await CallWithCatalogueResult(malformed);

        result.Should().Be(malformed);
    }

    [Fact]
    public async Task The_full_description_remains_available_to_the_ledger()
    {
        var ledger = new McpToolAnswerLedger();
        var recordingGuard = new ToolResultRecordingGuard(ledger);
        var fullAnswer = FullAnswer(LongDescription);

        await recordingGuard.InspectAsync(
            new ToolInvocation(
                CatalogueSearchToolNames.NameSearch,
                new Dictionary<string, object?> { ["category"] = "desk" }),
            fullAnswer,
            CancellationToken.None);

        await CallWithCatalogueResult(fullAnswer);

        ledger.RecordedAnswers.Should().ContainSingle();

        var recordedDescription = JsonNode.Parse(ledger.RecordedAnswers[0].AnswerText)!
            ["matches"]!["value"]!.AsArray()[0]!["description"]!
            .GetValue<string>();

        recordedDescription.Should().Be(LongDescription);
    }

    [Fact]
    public async Task An_emoji_near_the_limit_is_not_split()
    {
        var description = string.Concat(Enumerable.Repeat("😀", 120));
        var result = await CallWithCatalogueResult(FullAnswer(description));

        var limited = ProductOf(result)["description"]!.GetValue<string>();

        new StringInfo(limited).LengthInTextElements.Should().Be(100);
        limited.Should().NotContain("\uFFFD");
    }

    private static async Task<string> CallWithCatalogueResult(string resultText)
        => await CallWithResult(CatalogueSearchToolNames.NameSearch, resultText);

    private static async Task<string> CallWithResult(string toolName, object result)
    {
        var inner = new MessageCapturingChatClient();
        var client = new CatalogueToolDescriptionLimitChatClient(
            inner,
            NullLogger<CatalogueToolDescriptionLimitChatClient>.Instance);

        await client.GetResponseAsync(
        [
            new ChatMessage(
                ChatRole.Assistant,
                [new FunctionCallContent("call-1", toolName)]),
            new ChatMessage(
                ChatRole.Tool,
                [new FunctionResultContent("call-1", result)]),
        ]);

        return inner.Messages
            .SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Single()
            .Result!
            .ToString()!;
    }

    private static JsonObject ProductOf(string result)
        => JsonNode.Parse(result)!["matches"]!["value"]!.AsArray()[0]!.AsObject();

    private static string FullAnswer(string description)
        => JsonSerializer.Serialize(new
        {
            matches = new
            {
                value = new[]
                {
                    new
                    {
                        sku = "DSKB0B6MCK87",
                        name = "HON Mod Desk Shell, 60 x 30 x 29, Slate Teak",
                        category = "Desk",
                        subCategory = (string?)null,
                        monthlyPrice = new { amount = 271000m, currency = "IDR" },
                        description,
                        imagePath = "/images/vendored/DSKB0B6MCK87.jpg",
                        imageAvailable = true,
                        isFeatured = false,
                    },
                },
                count = 1,
                total = 1,
                truncated = false,
            },
            cheapestProductIgnoringTheCeiling = (decimal?)null,
        });

    private sealed class MessageCapturingChatClient : IChatClient
    {
        public IReadOnlyList<ChatMessage> Messages { get; private set; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Messages = messages.ToArray();

            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "{}")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Messages = messages.ToArray();
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "{}");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
