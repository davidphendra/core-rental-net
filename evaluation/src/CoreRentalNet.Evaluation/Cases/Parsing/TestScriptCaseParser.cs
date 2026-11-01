using System.Text.RegularExpressions;

using CoreRentalNet.Evaluation.Cases.Model;
using CoreRentalNet.Evaluation.Cases.Validation;
namespace CoreRentalNet.Evaluation.Cases.Parsing;

/// <summary>Parses every row of Test_Script.md into one evaluation case per row.</summary>
/// <remarks>
/// No row is merged or dropped. Rows that repeat a sentence and setup are separate test cases with a duplicate
/// group, and rows that repeat a sentence with different setups keep every acceptable variant.
/// </remarks>
public sealed class TestScriptCaseParser(ProductSkuResolver skuResolver)
{
    private static readonly Regex RowPattern = new(
        @"^\|\s*(?<row>\d+)\s*\|\s*""(?<sentence>.+?)""\s*\|\s*(?<setup>.+?)\s*\|$",
        RegexOptions.Multiline);

    private static readonly Regex SkuPattern = new(@"\((?<sku>[A-Z]{3}B[0-9A-Z]{8})\)");

    public EvaluationCaseSet Parse(string markdown)
    {
        var rows = RowPattern.Matches(markdown)
            .Select(ToRow)
            .ToArray();

        var variantsBySentence = rows
            .GroupBy(row => row.Sentence, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => row.Setup)
                    .DistinctBy(setup => setup.Key, StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        var duplicateGroups = rows
            .GroupBy(row => $"{row.Sentence}\u001f{row.Setup.Key}", StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select((group, index) => (group, id: $"DUP-{index + 1:000}"))
            .SelectMany(pair => pair.group.Select(row => (row.RowNumber, pair.id)))
            .ToDictionary(pair => pair.RowNumber, pair => pair.id);

        var ambiguityGroups = variantsBySentence
            .Where(pair => pair.Value.Length > 1)
            .Select((pair, index) => (pair.Key, id: $"AMB-{index + 1:000}"))
            .ToDictionary(pair => pair.Key, pair => pair.id, StringComparer.Ordinal);

        var cases = rows
            .Select(row => new EvaluationCase(
                RowNumber: row.RowNumber,
                CaseId: $"TS-{row.RowNumber:000}",
                Sentence: row.Sentence,
                Variants: variantsBySentence[row.Sentence],
                DuplicateGroup: duplicateGroups.GetValueOrDefault(row.RowNumber),
                AmbiguityGroup: ambiguityGroups.GetValueOrDefault(row.Sentence)))
            .ToArray();

        return new EvaluationCaseSet(cases);
    }

    private (int RowNumber, string Sentence, ExpectedSetupVariant Setup) ToRow(Match match)
    {
        var rowNumber = int.Parse(match.Groups["row"].Value);
        var sentence = match.Groups["sentence"].Value.Trim();
        var setup = ParseSetup(match.Groups["setup"].Value.Trim());

        return (rowNumber, sentence, setup);
    }

    private ExpectedSetupVariant ParseSetup(string setup)
        => new([.. setup
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => ParseLine(part.Trim()))]);

    private ExpectedLine ParseLine(string part)
    {
        var separator = part.IndexOf(':');
        var label = part[..separator].Trim();
        var body = part[(separator + 1)..].Trim();
        var slot = EvaluationSlot.Canonicalize(label);

        var skus = SkuPattern.Matches(body) is { Count: > 0 } skuMatches
            ? skuMatches.Select(match => match.Groups["sku"].Value).ToArray()
            : body.Split('+', StringSplitOptions.RemoveEmptyEntries)
                .Select(name => skuResolver.Resolve(name.Trim()))
                .ToArray();

        return new ExpectedLine(slot, skus);
    }
}
