using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.Model;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class OpenAiCompatibleChatClientFactoryTests
{
    [Fact]
    public void A_local_factory_builds_a_chat_client()
    {
        var factory = new OpenAiCompatibleChatClientFactory(
            new Uri("http://localhost:8080/v1"),
            "local",
            "Mistral-Small-3.1-24B-Instruct-2503",
            new ModelTransportRetryOptions());

        var client = factory.Create();

        client.Should().NotBeNull();
    }
}
