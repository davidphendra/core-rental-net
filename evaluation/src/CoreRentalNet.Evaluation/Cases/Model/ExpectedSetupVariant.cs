using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>One acceptable expected setup for a sentence.</summary>
public sealed record ExpectedSetupVariant(IReadOnlyList<ExpectedLine> Lines)
{
    public IReadOnlySet<string> Skus { get; } = Lines
        .SelectMany(line => line.Skus)
        .ToHashSet(StringComparer.Ordinal);

    public IReadOnlySet<string> Slots { get; } = Lines
        .Select(line => line.Slot)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>A stable key, so duplicate rows with the same setup can be recognised.</summary>
    public string Key { get; } = string.Join(
        ';',
        Lines
            .OrderBy(line => line.Slot, StringComparer.Ordinal)
            .Select(line => $"{line.Slot}:{string.Join('+', line.Skus.Order(StringComparer.Ordinal))}"));

    public IReadOnlyList<string> SkusFor(string slot)
        => Lines.FirstOrDefault(line => string.Equals(line.Slot, slot, StringComparison.Ordinal))?.Skus ?? [];

    public bool Matches(IReadOnlyList<Candidate> candidates)
        => candidates.Any(Matches);

    public bool Matches(Candidate candidate)
    {
        var expected = Lines.ToDictionary(
            line => line.Slot,
            line => line.Skus.ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);

        var actual = candidate.Lines
            .GroupBy(line => line.Slot, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(line => line.Sku).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        return expected.Count == actual.Count
            && expected.All(pair =>
                actual.TryGetValue(pair.Key, out var actualSkus)
                && pair.Value.SetEquals(actualSkus));
    }

    public double F1(IReadOnlyList<Candidate> candidates)
        => candidates.Count == 0 ? 0 : candidates.Max(F1);

    public double F1(Candidate candidate)
    {
        var expected = Lines
            .SelectMany(line => line.Skus.Select(sku => (line.Slot, Sku: sku)))
            .ToHashSet();

        var actual = candidate.Lines
            .Select(line => (line.Slot, line.Sku))
            .ToHashSet();

        if (expected.Count == 0 && actual.Count == 0)
        {
            return 1;
        }

        var intersection = expected.Intersect(actual).Count();
        return 2d * intersection / (expected.Count + actual.Count);
    }
}
