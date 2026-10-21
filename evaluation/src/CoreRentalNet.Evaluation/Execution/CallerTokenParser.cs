using System.Text.Json;

namespace CoreRentalNet.Evaluation.Execution;

/// <summary>Reads the claims of a JWT access token. Signature validation belongs to the resource server.</summary>
public static class CallerTokenParser
{
    /// <summary>The token value and the non-secret claims a preflight can inspect.</summary>
    public static CallerToken Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var parts = value.Split('.');
        if (parts.Length < 2)
        {
            throw new CallerTokenException("The caller token is not a JWT.");
        }

        try
        {
            using var document = JsonDocument.Parse(Decode(parts[1]));
            var payload = document.RootElement;

            return new CallerToken(
                value,
                ExpiresAt(payload),
                String(payload, "iss"),
                Strings(payload, "aud"),
                Strings(payload, "permissions"));
        }
        catch (JsonException exception)
        {
            throw new CallerTokenException("The caller token payload is not valid JSON.", exception);
        }
        catch (FormatException exception)
        {
            throw new CallerTokenException("The caller token payload is not valid base64url.", exception);
        }
    }

    private static DateTimeOffset? ExpiresAt(JsonElement payload)
        => payload.TryGetProperty("exp", out var expiry) && expiry.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

    private static string? String(JsonElement payload, string name)
        => payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static IReadOnlyList<string> Strings(JsonElement payload, string name)
    {
        if (!payload.TryGetProperty(name, out var value))
        {
            return [];
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => [value.GetString()!],
            JsonValueKind.Array => [.. value.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString()!)],
            _ => [],
        };
    }

    private static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');

        return Convert.FromBase64String(padded);
    }
}
