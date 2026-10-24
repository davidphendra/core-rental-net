using System.Text.Json;

namespace CoreRentalNet.Evaluation.Evaluation;

/// <summary>Parses and validates the local judge's JSON response.</summary>
public sealed class JudgeVerdictParser
{
    public JudgeEvaluation Parse(string? responseText)
    {
        var json = ExtractJsonObject(responseText);
        var dto = JsonSerializer.Deserialize<JudgeVerdictDto>(json, EvaluationJson.Options)
            ?? throw new InvalidOperationException("The judge returned no JSON.");

        if (!string.Equals(dto.Verdict, "pass", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(dto.Verdict, "fail", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The judge returned an unknown verdict.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new InvalidOperationException("The judge returned no reason.");
        }

        return new JudgeEvaluation(
            Evaluated: true,
            Passed: string.Equals(dto.Verdict, "pass", StringComparison.OrdinalIgnoreCase),
            Confidence: Math.Clamp(dto.Confidence, 0, 1),
            Reason: dto.Reason);
    }

    private static string ExtractJsonObject(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("The judge returned an empty response.");
        }

        var start = responseText.IndexOf('{', StringComparison.Ordinal);
        var end = responseText.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("The judge returned no JSON object.");
        }

        return responseText[start..(end + 1)];
    }
}
