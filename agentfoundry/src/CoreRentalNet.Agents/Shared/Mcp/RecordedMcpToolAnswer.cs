namespace CoreRentalNet.Agents.Shared.Mcp;

/// <summary>One answer an MCP tool gave, with the arguments it was asked with.</summary>
/// <remarks>
/// <b>The tool's own text, unaltered.</b> Nothing here was written by a model: the arguments are the ones the
/// function loop was called with, and the answer is what came back from the tool. That is what lets a later stage
/// reason over a product's description without asking a model to re-type it — a model copying six hundred
/// characters can truncate, paraphrase or fuse them, and nothing downstream would notice.
/// </remarks>
/// <param name="ToolName">The tool's own name, as it published it.</param>
/// <param name="Arguments">What the tool was called with, as a copy: the arguments dictionary is reused by the
/// function loop, so holding the original would be holding something still being written.</param>
/// <param name="AnswerText">What the tool answered, as text.</param>
public sealed record RecordedMcpToolAnswer(
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    string AnswerText);
