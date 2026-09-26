using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;

namespace CoreRentalNet.Modules.Workspace.UnitTests.Suggestions;

/// <summary>A hand-written record writer: it keeps every record a run produced.</summary>
internal sealed class RecordingWorkspaceSuggestionRunRecordWriter : IWorkspaceSuggestionRunRecordWriter
{
    public List<WorkspaceSuggestionRunRecord> Records { get; } = [];

    public void WriteRunRecord(WorkspaceSuggestionRunRecord suggestionRunRecord) => Records.Add(suggestionRunRecord);
}
