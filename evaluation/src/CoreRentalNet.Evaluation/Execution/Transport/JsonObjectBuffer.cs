using System.Text;

using CoreRentalNet.Evaluation.Execution.Auth;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;
namespace CoreRentalNet.Evaluation.Execution.Transport;

/// <summary>Reassembles complete JSON objects from a stream of text fragments.</summary>
public sealed class JsonObjectBuffer
{
    private readonly StringBuilder _buffer = new();

    public IEnumerable<string> Append(string? text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _buffer.Append(text);
        }

        while (TryTake(out var json))
        {
            yield return json;
        }
    }

    public IEnumerable<string> Flush()
    {
        var remainder = _buffer.ToString().Trim();
        _buffer.Clear();

        if (remainder.Length > 0)
        {
            yield return remainder;
        }
    }

    private bool TryTake(out string json)
    {
        var text = _buffer.ToString();
        var start = text.IndexOf('{');

        if (start < 0)
        {
            _buffer.Clear();
            json = string.Empty;
            return false;
        }

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var index = start; index < text.Length; index++)
        {
            var character = text[index];

            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            switch (character)
            {
                case '"':
                    inString = true;
                    break;

                case '{':
                    depth++;
                    break;

                case '}':
                    depth--;

                    if (depth == 0)
                    {
                        json = text[start..(index + 1)];
                        _buffer.Remove(0, index + 1);
                        return true;
                    }

                    break;
            }
        }

        json = string.Empty;
        return false;
    }
}
