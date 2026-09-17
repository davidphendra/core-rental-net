namespace AgentFoundry.WorkspaceSuggestions.Intent;

/// <summary>
/// Decides whether a request is about composing a workspace at all - the run's cheapest gate.
/// </summary>
/// <remarks>
/// A port rather than a call to a model, so the decision is testable without one and so the model that
/// answers it is a deployment choice. The verifier runs first because it is the only node that needs no
/// catalogue: refusing before anything is read is what makes a refusal cost one short call rather than
/// a whole run.
/// </remarks>
public interface IIntentClassifier
{
    Task<IntentVerdict> ClassifyAsync(string query, CancellationToken cancellationToken = default);
}
