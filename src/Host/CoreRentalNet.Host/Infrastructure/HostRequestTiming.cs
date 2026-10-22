using System.Globalization;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>Named durations for one request, emitted as a Server-Timing header.</summary>
/// <remarks>
/// Scoped to the MCP request. The header is for local diagnosis and is harmless on other requests; tool and
/// search timings are recorded here rather than inferred from logs.
/// </remarks>
public sealed class HostRequestTiming
{
    private readonly Dictionary<string, double> _milliseconds = new(StringComparer.Ordinal);

    public void Record(string name, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _milliseconds[name] = duration.TotalMilliseconds;
    }

    public string Header(TimeSpan total)
    {
        var entries = new List<string>
        {
            $"mcp;dur={Format(total.TotalMilliseconds)}",
        };

        entries.AddRange(_milliseconds
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key};dur={Format(pair.Value)}"));

        return string.Join(", ", entries);
    }

    private static string Format(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);
}
