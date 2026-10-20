using System.Text.RegularExpressions;

namespace CoreRentalNet.Evaluation.Cases;

/// <summary>Resolves a product name from the test script to a catalogue SKU.</summary>
/// <remarks>
/// Only row 24 has no SKU. Its two names are resolved by requiring every word of the requested name to appear
/// in exactly one catalogue name; an ambiguous or absent match is a fixture error, not a guess.
/// </remarks>
public sealed class ProductSkuResolver(IReadOnlyList<ProductRecord> products)
{
    public string Resolve(string productName)
    {
        var requestedTokens = Tokens(productName);

        var matches = products
            .Where(product => requestedTokens.All(token => Tokens(product.Name).Contains(token)))
            .ToArray();

        return matches.Length switch
        {
            1 => matches[0].SkuNo,
            0 => throw new InvalidOperationException(
                $"No catalogue product matches '{productName}'."),
            _ => throw new InvalidOperationException(
                $"'{productName}' matches {matches.Length} catalogue products: " +
                string.Join(", ", matches.Select(product => product.SkuNo))),
        };
    }

    private static HashSet<string> Tokens(string value)
        => Regex.Split(value.ToLowerInvariant(), "[^a-z0-9]+")
            .Where(token => token.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
}
