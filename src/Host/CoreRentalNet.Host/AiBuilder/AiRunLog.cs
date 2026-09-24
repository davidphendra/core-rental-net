using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CoreRentalNet.Host.AiBuilder;

/// <summary>Where a run's record is written: one line per run, and one when one of its candidates is applied.</summary>
/// <remarks>
/// <para>
/// A structured line rather than a row, and that is a decision the epic does not make for us - recorded rather
/// than hidden. The story asks for "one structured record per run" with "bounded retention of 90 days"; the
/// application has no store for it, and the two contexts it has belong to modules whose data this is not. So
/// the record is written where this application already writes its operational facts: as JSON on the logger,
/// under a category of its own, where the retention policy is the deployment's log policy. A store would need
/// a home, a table and a cleanup - none of which any task in this epic provides.
/// </para>
/// <para>
/// The whole record travels as one JSON object on the message, because the fields have to survive to a log
/// sink intact: a sink that reads them as properties is then free to index them, and a reader who sees the
/// line as text still sees everything.
/// </para>
/// </remarks>
internal static class AiRunLog
{
    /// <summary>The logger category the lines are written under, so a test can find them.</summary>
    public const string Category = "CoreRentalNet.Host.AiBuilder.AiRunLog";

    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web)
    {
        // The verdict is read by a person looking at a log line, not by this application parsing its own
        // output, so it is written as the word rather than as the number an enum defaults to.
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>One line describing a run.</summary>
    public static void Ran(ILogger logger, AiRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(record);

        logger.LogInformation("Suggestion run {RunId}: {Record}", record.RunId, JsonSerializer.Serialize(record, Wire));
    }

    /// <summary>One line saying that a run's candidate was chosen, joined to it by the run's id.</summary>
    public static void Applied(ILogger logger, AiRunApplication application)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(application);

        logger.LogInformation(
            "Suggestion run {RunId}: {Application}",
            application.RunId,
            JsonSerializer.Serialize(application, Wire));
    }
}
