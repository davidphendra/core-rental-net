using CoreRentalNet.BuildingBlocks.Domain;

namespace CoreRentalNet.Modules.Catalog.Domain;

/// <summary>A stock keeping unit code. Case-insensitive input, stored upper case.</summary>
public sealed record Sku
{
    public const int MaxLength = 32;

    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Sku Of(string? value)
        => TryParse(value, out var sku) ? sku : throw new DomainRuleViolationException(
            $"'{value}' is not a valid SKU. A SKU must be 1 to {MaxLength} letters or digits.");

    public static bool TryParse(string? value, out Sku sku)
    {
        sku = null!;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length > MaxLength || !normalized.All(char.IsAsciiLetterOrDigit))
        {
            return false;
        }

        sku = new Sku(normalized);
        return true;
    }

    public override string ToString() => Value;
}
