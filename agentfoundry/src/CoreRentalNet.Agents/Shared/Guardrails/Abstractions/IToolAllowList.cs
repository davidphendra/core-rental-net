namespace CoreRentalNet.Agents.Shared.Guardrails.Abstractions;

/// <summary>The tools this deployment permits — one list, read by the offer filter and the guard alike.</summary>
/// <remarks>
/// One interface, two readers: the client that decides which tools the model is offered, and the guard that
/// refuses anything else at execution. A second list would be free to disagree with the first.
/// </remarks>
internal interface IToolAllowList
{
    bool Contains(string toolName);
}
