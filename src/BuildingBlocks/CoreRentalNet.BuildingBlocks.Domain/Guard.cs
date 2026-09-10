namespace CoreRentalNet.BuildingBlocks.Domain;

/// <summary>Small validation helpers used by value objects to keep their invariants.</summary>
public static class Guard
{
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

    public static T NotNull<T>(T? value, string fieldName)
        where T : class
        => value ?? throw new DomainRuleViolationException($"{fieldName} is required.");

    public static int Positive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new DomainRuleViolationException($"{fieldName} must be positive, but was {value}.");
        }

        return value;
    }
}
