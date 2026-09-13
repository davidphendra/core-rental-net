using System.Diagnostics;

namespace CoreRentalNet.E2E;

/// <summary>
/// A started process, with enough of its output kept to explain a failed start.
/// </summary>
/// <remarks>
/// The last lines are kept rather than the whole stream: a host writes far more than a failure needs,
/// and what matters when a start fails is what it said at the end.
/// </remarks>
internal sealed class AppProcess(Process process)
{
    private const int KeptLines = 40;

    private readonly Queue<string> output = new();

    public bool HasExited => process.HasExited;

    public int ExitCode => process.ExitCode;

    public void Record(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (output)
        {
            output.Enqueue(line);

            while (output.Count > KeptLines)
            {
                output.Dequeue();
            }
        }
    }

    /// <summary>What the process said, for a failure message. Never empty.</summary>
    public string Output()
    {
        lock (output)
        {
            return output.Count == 0
                ? "(the process said nothing)"
                : string.Join(Environment.NewLine, output);
        }
    }

    public async Task StopAsync()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().ConfigureAwait(false);
        }

        process.Dispose();
    }
}
