namespace CoreRentalNet.Agents.Shared.ChatClients;

/// <summary>The phase of a model call that can shape its prompt and its response grammar.</summary>
/// <remarks>
/// A retriever makes two calls: one that chooses tools and one that writes the structured answer after the tool
/// results arrive. Naming the phase is what lets telemetry separate their costs and lets the final call receive
/// a compact tool-result projection.
/// </remarks>
internal static class ChatCallPhase
{
    public const string Key = "chat.call.phase";

    public const string ToolPlanning = "tool-planning";

    public const string FinalStructured = "final-structured";
}
