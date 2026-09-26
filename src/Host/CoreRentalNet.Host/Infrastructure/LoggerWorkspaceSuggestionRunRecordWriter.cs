using System.Text.Json;
using System.Text.Json.Serialization;
using CoreRentalNet.Modules.Workspace.Application.Suggestions.Records;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>Writes a finished run's record as one structured log line, under a category of its own.</summary>
/// <remarks>
/// A structured line rather than a row: the application has no store for it, and the two contexts it has belong
/// to modules whose data this is not. So the record is written where this application already writes its
/// operational facts, as JSON on the logger, under a category of its own. The whole record travels as one JSON
/// object on the message, so a sink that reads the fields can index them and a reader who sees the line as text
/// still sees everything.
/// </remarks>
internal sealed class LoggerWorkspaceSuggestionRunRecordWriter(ILoggerFactory loggerFactory)
    : IWorkspaceSuggestionRunRecordWriter
{
    /// <summary>The logger category the lines are written under, so a test can find them.</summary>
    public const string Category = "CoreRentalNet.Host.Infrastructure.LoggerWorkspaceSuggestionRunRecordWriter";

    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        // The verdict is read by a person looking at a log line, not by this application parsing its own
        // output, so it is written as the word rather than as the number an enum defaults to.
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public void WriteRunRecord(WorkspaceSuggestionRunRecord suggestionRunRecord)
    {
        ArgumentNullException.ThrowIfNull(suggestionRunRecord);

        loggerFactory.CreateLogger(Category).LogInformation(
            "Suggestion run {SuggestionRunId}: {SuggestionRunRecord}",
            suggestionRunRecord.RunId,
            JsonSerializer.Serialize(suggestionRunRecord, Wire));
    }
}
