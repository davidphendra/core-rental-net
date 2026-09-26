namespace CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;

/// <summary>Where a finished run is written, so the use case does not depend on a logging framework.</summary>
/// <remarks>
/// A port, so the run service is testable with a recording fake and the writing itself - a structured log
/// line, in this deployment - stays a composition-root choice.
/// </remarks>
public interface IWorkspaceSuggestionRunRecordWriter
{
    void WriteRunRecord(WorkspaceSuggestionRunRecord suggestionRunRecord);
}
