namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>Validation a value object applies to keep its invariants, so the message is stated once.</summary>
public static class Guard
{
    /// <summary>A trimmed, non-empty value of at most <paramref name="maxLength"/> characters.</summary>
    /// <exception cref="DomainRuleViolationException">
    /// The value is null, empty or whitespace, or its trimmed length exceeds the maximum.
    /// </exception>
    public static string NotEmpty(string? value, string fieldName, int maxLength = 512)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleViolationException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new DomainRuleViolationException(
                $"{fieldName} cannot be longer than {maxLength} characters, but was {trimmed.Length}.");
        }

        return trimmed;
    }
}
