using Microsoft.Extensions.AI;

namespace AgentFoundry.Tests;

/// <summary>A model that says what a test told it to, and remembers what it was asked.</summary>
/// <remarks>
/// <para>
/// Hand-written rather than mocked, like every other fake in this tree. It keeps the whole prompt it
/// received, because most of what these tests assert about is what a model would have been looking at
/// when it answered.
/// </para>
/// <para>
/// Given several answers it gives them in order and then keeps repeating the last, which is what a run
/// needs: the first call is a verdict, the second is a classification, and a retry asks the same thing
/// again and should get the same kind of answer.
/// </para>
/// </remarks>
internal sealed class ScriptedChatClient(params string[] answers) : IChatClient
{
    private readonly Queue<string> pending = new(answers);
    private string last = string.Empty;

    public string? Asked { get; private set; }

    public int Calls { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Asked = string.Join("\n", messages.Select(message => message.Text));
        Calls++;

        last = pending.Count > 0 ? pending.Dequeue() : last;

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, last)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("these classifiers ask for whole answers");

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        // Nothing to release: the model is a string.
    }
}
