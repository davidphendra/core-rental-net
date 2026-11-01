using System.Globalization;

using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
namespace CoreRentalNet.Evaluation.Evaluation.Timing;

/// <summary>Reads the standard Server-Timing header into named millisecond values.</summary>
public static class ServerTiming
{
    public static IReadOnlyDictionary<string, double> Parse(string? value)
    {
        var values = new Dictionary<string, double>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(value))
        {
            return values;
        }

        foreach (var entry in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = entry.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                continue;
            }

            var duration = parts
                .Skip(1)
                .FirstOrDefault(part => part.StartsWith("dur=", StringComparison.OrdinalIgnoreCase));

            if (duration is not null
                && double.TryParse(
                    duration["dur=".Length..],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var milliseconds))
            {
                values[parts[0]] = milliseconds;
            }
        }

        return values;
    }
}
