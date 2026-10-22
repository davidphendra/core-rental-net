using CoreRentalNet.Agents.Shared.ChatClients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRentalNet.Agents.Shared.Evaluation;

/// <summary>Adds the opt-in evaluation trace without changing any feature.</summary>
internal static class EvaluationTraceRegistration
{
    /// <summary>The directory a deployment sets to enable evaluation tracing.</summary>
    public const string DirectoryKey = "EVALUATION_TRACE_PATH";

    public static IServiceCollection AddEvaluationTrace(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var directory = configuration[DirectoryKey]?.Trim();

        // Disabled means no sink, no decorator, and no behaviour change at all.
        if (string.IsNullOrWhiteSpace(directory))
        {
            return services;
        }

        services.AddScoped<IEvaluationTraceSink>(_ =>
            new JsonlEvaluationTraceSink(new EvaluationTraceOptions(directory)));

        // Construct the listener now: a lazily resolved singleton would never be created, and no spans would
        // be captured. The container still owns it and disposes it with the host.
        services.AddSingleton(new EvaluationSpanCollector(directory));

        services.Decorate<ITelemetryChatClient>((innerClient, provider) =>
            new EvaluationTraceChatClient(
                innerClient,
                provider.GetRequiredService<IEvaluationTraceSink>()));

        return services;
    }
}
