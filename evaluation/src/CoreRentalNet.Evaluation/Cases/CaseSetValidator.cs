namespace CoreRentalNet.Evaluation.Cases;

/// <summary>Checks the parsed script against the catalogue before a single model call is spent.</summary>
public sealed class CaseSetValidator
{
    public CaseSetValidation Validate(
        EvaluationCaseSet caseSet,
        IReadOnlyDictionary<string, ProductRecord> catalogue)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (caseSet.Cases.Count != 200)
        {
            errors.Add($"Expected 200 rows, found {caseSet.Cases.Count}.");
        }

        foreach (var testCase in caseSet.Cases)
        {
            if (testCase.Variants.Count == 0)
            {
                errors.Add($"{testCase.CaseId}: no expected setup.");
                continue;
            }

            if (!testCase.RequestedSlots.Contains(EvaluationSlot.Desk))
            {
                errors.Add($"{testCase.CaseId}: no desk.");
            }

            if (!testCase.RequestedSlots.Contains(EvaluationSlot.Chair)
                && !testCase.RequestedSlots.Contains(EvaluationSlot.RelaxZone))
            {
                errors.Add($"{testCase.CaseId}: no seating (chair or bean bag).");
            }

            foreach (var variant in testCase.Variants)
            {
                foreach (var line in variant.Lines)
                {
                    foreach (var sku in line.Skus)
                    {
                        if (!catalogue.TryGetValue(sku, out var product))
                        {
                            errors.Add($"{testCase.CaseId}: SKU '{sku}' is not in the catalogue.");
                            continue;
                        }

                        var expected = CataloguePositionFor(line.Slot);
                        if (!string.Equals(product.Category, expected.Category, StringComparison.Ordinal)
                            || !string.Equals(product.SubCategory, expected.SubCategory, StringComparison.Ordinal))
                        {
                            errors.Add(
                                $"{testCase.CaseId}: SKU '{sku}' is {product.Category}/{product.SubCategory}, " +
                                $"expected {expected.Category}/{expected.SubCategory} for {line.Slot}.");
                        }
                    }
                }
            }
        }

        if (caseSet.UniqueSentenceCount != 58)
        {
            warnings.Add($"Expected 58 unique sentences, found {caseSet.UniqueSentenceCount}.");
        }

        return new CaseSetValidation(errors.Count == 0, errors, warnings);
    }

    private static (string Category, string? SubCategory) CataloguePositionFor(string slot)
        => slot switch
        {
            EvaluationSlot.Desk => ("desk", null),
            EvaluationSlot.Chair => ("chair", null),
            EvaluationSlot.Monitor => ("accessory", "monitor"),
            EvaluationSlot.Lamp => ("accessory", "lamp"),
            EvaluationSlot.Plant => ("accessory", "plant"),
            EvaluationSlot.RelaxZone => ("accessory", "beanbag"),
            EvaluationSlot.CoffeeStation => ("accessory", "coffee"),
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown workspace slot."),
        };
}
