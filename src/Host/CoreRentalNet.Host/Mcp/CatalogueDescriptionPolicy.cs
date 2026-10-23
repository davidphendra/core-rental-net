using System.Globalization;
using System.Text;
using CoreRentalNet.Host.Configs;

namespace CoreRentalNet.Host.Mcp;

/// <summary>The one rule for a description published by a catalogue MCP tool.</summary>
/// <remarks>
/// The REST answer keeps the catalogue's full description; a tool answer is model context and carries only the
/// bounded form. The cut keeps the result at most 150 UTF-16 characters while never splitting a Unicode text
/// element, so a surrogate pair or combining sequence is not broken.
/// </remarks>
internal static class CatalogueDescriptionPolicy
{
    public static string Truncate(string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return string.Empty;
        }

        if (description.Length <= CatalogToolLimits.MaximumDescriptionCharacterCount)
        {
            return description;
        }

        var bound = new StringBuilder(CatalogToolLimits.MaximumDescriptionCharacterCount);
        var textElements = StringInfo.GetTextElementEnumerator(description);

        while (textElements.MoveNext())
        {
            var element = textElements.GetTextElement();

            if (bound.Length + element.Length > CatalogToolLimits.MaximumDescriptionCharacterCount)
            {
                break;
            }

            bound.Append(element);
        }

        return bound.ToString();
    }
}
