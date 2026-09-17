using System.Text.Json;
using AgentFoundry.WorkspaceSuggestions.Prompts;
using AgentFoundry.WorkspaceSuggestions.Vocabularies;
using Microsoft.Extensions.AI;

namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>
/// Asks a model whether a request is about furnishing a workspace.
/// </summary>
/// <remarks>
/// The workflow's first stage, and the gate the rest of the run sits behind. It is given the customer's
/// words and nothing else - no catalogue, no slot rules, no findings - so there is nothing it could be
/// misled by and nothing it could reason from that has not been said.
/// </remarks>
internal sealed class IntentClassifier(IChatClient client, PromptLibrary prompts) : IIntentClassifier
{
    public async Task<IntentVerdict> ClassifyAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var prompt = prompts.Render("workspace-intent", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["query"] = query,
        });

        using var document = JsonDocument.Parse(
            await PromptAnswer.JsonAsync(client, prompt, cancellationToken).ConfigureAwait(false));

        return Verdict(document.RootElement);
    }

    /// <summary>
    /// The model's answer, reduced to one of the two verdicts the contract has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reason code is decided here and the model's own is never read, which is the whole point of
    /// doing it in this method. The result schema's <c>code</c> enum holds exactly one value for a
    /// refusal, and the workflow writes whatever the verdict carries straight into the result - so a
    /// model that answered with a word of its own would produce a result the contract forbids, and that
    /// schema is the only thing the application and the agent share.
    /// </para>
    /// <para>
    /// An answer without the field is refused rather than read as a no. A missing field is a broken
    /// answer, not an opinion, and turning it into a refusal would refuse a customer on the strength of
    /// something the model never said.
    /// </para>
    /// </remarks>
    private static IntentVerdict Verdict(JsonElement answer)
    {
        if (answer.ValueKind != JsonValueKind.Object
            || !answer.TryGetProperty("isWorkspaceRequest", out var said)
            || said.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new PromptAnswerException("The model's answer did not say whether the request was one.");
        }

        return said.ValueKind == JsonValueKind.True
            ? new IntentVerdict(IsWorkspaceRequest: true, ReasonCodes.Ok)
            : new IntentVerdict(IsWorkspaceRequest: false, ReasonCodes.NotWorkspaceRequest);
    }
}
