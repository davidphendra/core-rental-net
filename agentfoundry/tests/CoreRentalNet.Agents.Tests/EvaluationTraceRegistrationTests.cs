using AwesomeAssertions;
using CoreRentalNet.Agents.Shared.ChatClients;
using CoreRentalNet.Agents.Shared.Evaluation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreRentalNet.Agents.Tests;

public sealed class EvaluationTraceRegistrationTests
{
    [Fact]
    public void Tracing_is_a_no_op_when_no_path_is_configured()
    {
        using var provider = Build(new Dictionary<string, string?>());
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITelemetryChatClient>()
            .Should().BeOfType<FakeTelemetryChatClient>();
    }

    [Fact]
    public void Tracing_decorates_the_telemetry_client_when_a_path_is_configured()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            [EvaluationTraceRegistration.DirectoryKey] = Path.Combine(
                Path.GetTempPath(), $"evaluation-trace-{Guid.NewGuid():N}"),
        });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<ITelemetryChatClient>()
            .Should().BeOfType<EvaluationTraceChatClient>();
    }

    private static ServiceProvider Build(IReadOnlyDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddScoped<ITelemetryChatClient, FakeTelemetryChatClient>();
        services.AddEvaluationTrace(configuration);

        return services.BuildServiceProvider();
    }
}
