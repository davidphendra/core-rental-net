using CoreRentalNet.Agents.Shared.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace CoreRentalNet.Agents.Shared.Telemetry;

/// <summary>Stamps the build identity on every log record.</summary>
/// <remarks>
/// The hosting package sets the logger provider's resource after ours, so a resource contributed through the
/// logger builder would be replaced. A processor runs on each record instead, so the identity reaches logs
/// without fighting that, and it is read from the artifact rather than from the environment.
/// </remarks>
internal sealed class AgentBuildIdentityLogProcessor : BaseProcessor<LogRecord>
{
    private readonly IReadOnlyList<KeyValuePair<string, object?>> _identity;

    public AgentBuildIdentityLogProcessor(AgentBuildIdentity identity)
        => _identity = identity.ResourceAttributes()
            .Select(attribute => new KeyValuePair<string, object?>(attribute.Key, attribute.Value))
            .ToList();

    public override void OnEnd(LogRecord data)
    {
        var keys = data.Attributes is null
            ? Enumerable.Empty<string>()
            : data.Attributes.Select(attribute => attribute.Key);
        var present = new HashSet<string>(keys, StringComparer.Ordinal);
        var missing = _identity.Where(attribute => !present.Contains(attribute.Key)).ToList();

        if (missing.Count == 0)
        {
            return;
        }

        List<KeyValuePair<string, object?>> attributes = data.Attributes is null
            ? new List<KeyValuePair<string, object?>>(missing)
            : [.. data.Attributes, .. missing];

        data.Attributes = attributes;
    }
}
