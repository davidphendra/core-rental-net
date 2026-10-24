using System.Text.Json;
using CoreRentalNet.Evaluation.Evidence;

namespace CoreRentalNet.Evaluation.Reporting.Bias;

/// <summary>Reads the few trace fields the reasoning-bias metrics need.</summary>
public static class TraceResponseReader
{
    public static IReadOnlySet<string>? RephraserSlots(RunTrace trace)
    {
        var response = LastResponse(trace, WorkspaceStageNames.Rephraser);

        if (response is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(response);
            var slots = new HashSet<string>(StringComparer.Ordinal);

            foreach (var category in document.RootElement.GetProperty("categories").EnumerateArray())
            {
                if (!category.TryGetProperty("key", out var key) || key.ValueKind is not JsonValueKind.String)
                {
                    continue;
                }

                try
                {
                    slots.Add(EvaluationComponentMapping.SlotForComponent(key.GetString()!));
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Unknown components are not counted as requested slots.
                }
            }

            return slots;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool? VerifierAccepted(RunTrace trace)
    {
        var response = LastResponse(trace, WorkspaceStageNames.Verifier);

        if (response is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(response);
            return document.RootElement.TryGetProperty("isWorkspaceRequest", out var accepted)
                   && accepted.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? accepted.GetBoolean()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool? ReviewerAccepted(RunTrace trace)
    {
        var response = LastResponse(trace, WorkspaceStageNames.Reviewer);

        if (response is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(response);
            return document.RootElement.TryGetProperty("isAcceptable", out var accepted)
                   && accepted.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? accepted.GetBoolean()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? LastResponse(RunTrace trace, string stage)
        => trace.StageCalls
            .LastOrDefault(call => string.Equals(call.Stage, stage, StringComparison.Ordinal))
            ?.ResponseText;
}
