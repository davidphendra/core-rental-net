using Microsoft.Extensions.AI;
using WorkspaceSuggestions.Contracts;

namespace WorkspaceSuggestions.Usage;

/// <summary>What one run cost, accumulated as it happens.</summary>
/// <remarks>
/// <para>
/// <b>This exists because the model cannot know these numbers.</b> They were once required fields of the
/// result the model produced, which meant a live run would have had the model invent a call count, a token
/// count, a deployment name and a prompt version - and validation would have passed, because integers are
/// integers. The schema said the call count "can never drift unnoticed" while the only party asked for it
/// had no way to observe it.
/// </para>
/// <para>
/// So they are counted here, from the framework's own reports, by the code that makes the calls. Every field
/// is therefore either observed or configured, and none is guessed.
/// </para>
/// </remarks>
internal sealed class UsageRecorder
{
    private int _calls;
    private long _input;
    private long _output;

    /// <summary>How many times a model was called. Two on the happy path, three with a repair loop.</summary>
    public int ModelCalls => Volatile.Read(ref _calls);

    public long InputTokens => Interlocked.Read(ref _input);

    public long OutputTokens => Interlocked.Read(ref _output);

    /// <summary>One call was made, whether or not it reported what it cost.</summary>
    public void Called() => Interlocked.Increment(ref _calls);

    /// <summary>Adds what a call reported. A client that reports nothing is not an error.</summary>
    public void Record(UsageDetails? usage)
    {
        if (usage is null)
        {
            return;
        }

        if (usage.InputTokenCount is { } input)
        {
            Interlocked.Add(ref _input, input);
        }

        if (usage.OutputTokenCount is { } output)
        {
            Interlocked.Add(ref _output, output);
        }
    }

    /// <summary>The run's cost, with the two facts the code knows rather than the model.</summary>
    public RunUsage Snapshot(string model, string promptVersion)
        => new(ModelCalls, Clamp(InputTokens), Clamp(OutputTokens), model, promptVersion);

    /// <summary>The contract carries token counts as integers; a run cannot reach the ceiling, but a bug
    /// that accumulated across runs could, and wrapping negative would be worse than saturating.</summary>
    private static int Clamp(long value) => value > int.MaxValue ? int.MaxValue : (int)value;
}
