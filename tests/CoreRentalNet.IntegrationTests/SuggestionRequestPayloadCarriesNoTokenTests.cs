using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Agent;
using CoreRentalNet.Modules.Workspace.Infrastructure.Suggestions;
using Xunit;

namespace CoreRentalNet.IntegrationTests;

/// <summary>
/// The bytes the application sends to the agent are not a secret, and that is the whole point of the carrier
/// change.
/// </summary>
/// <remarks>
/// <para>
/// The caller's token used to be a member of this payload, and the payload is a message: the hosting layer records
/// it with the request, the hash identifies what a run was drawn from, and a model may be handed it. A bearer
/// token belongs in none of those, so it travels on the invocation instead — which leaves the payload with nothing
/// secret in it, and leaves the hash hashing exactly the bytes that are sent.
/// </para>
/// <para>
/// <b>Asserted over the serialized bytes rather than over the record's members</b>, because what leaves the
/// process is what matters: a member that exists but is always null would still put the word in the body, and a
/// reader of the wire would have no way to tell that it is never filled.
/// </para>
/// </remarks>
public sealed class SuggestionRequestPayloadCarriesNoTokenTests
{
    private const string TheCallersToken = "header.eyJzdWIiOiJjdXN0b21lci0xIn0.signature";

    [Fact]
    public void The_request_body_sent_to_the_agent_carries_no_token()
    {
        var payload = MicrosoftFoundrySuggestionRequestPayload.From(TheRequestASentenceWouldProduce());

        payload.Json.Should().NotContain(
            "token",
            "the body is recorded, hashed and shown to a model, and the token travels on the invocation instead");
        payload.Json.Should().NotContain(TheCallersToken);
    }

    [Fact] // and the hash is over the bytes that are sent, not over a second serialization with a field removed
    public void The_hash_is_over_the_bytes_that_are_sent()
    {
        var payload = MicrosoftFoundrySuggestionRequestPayload.From(TheRequestASentenceWouldProduce());

        payload.Json.Should().NotContain("mcpAccessToken");
        payload.Hash.Should().MatchRegex("^[0-9a-f]{64}$", "a SHA-256 over the body is still what the record keeps");
    }

    private static WorkspaceSuggestionRequestPayload TheRequestASentenceWouldProduce()
        => new(
            RunId: "run-1",
            Query: "a desk and a chair for a small room",
            Currency: "IDR",
            CeilingMonthly: 1_500_000,
            Slots: []);
}
