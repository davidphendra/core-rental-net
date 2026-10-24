namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Shared bias formulas, so no section builder duplicates arithmetic.</summary>
public static class BiasStatistics
{
    public static double? Rate(int numerator, int denominator)
        => denominator == 0 ? null : numerator / (double)denominator;

    public static double? Difference(IReadOnlyCollection<double> values)
        => values.Count < 2 ? null : values.Max() - values.Min();

    public static double? Ratio(IReadOnlyCollection<double> values)
    {
        if (values.Count < 2)
        {
            return null;
        }

        var maximum = values.Max();
        return maximum <= 0 ? null : values.Min() / maximum;
    }

    public static double? ExposureDifference(
        IReadOnlyDictionary<string, int> expected,
        IReadOnlyDictionary<string, int> actual)
    {
        var slots = expected.Keys.Union(actual.Keys, StringComparer.Ordinal).ToArray();
        var expectedTotal = expected.Values.Sum();
        var actualTotal = actual.Values.Sum();

        if (slots.Length == 0 || expectedTotal == 0 || actualTotal == 0)
        {
            return null;
        }

        return slots.Max(slot =>
        {
            var expectedShare = expected.GetValueOrDefault(slot) / (double)expectedTotal;
            var actualShare = actual.GetValueOrDefault(slot) / (double)actualTotal;
            return Math.Abs(expectedShare - actualShare);
        });
    }
}
