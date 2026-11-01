using CoreRentalNet.Evaluation.Execution.Transport;
using CoreRentalNet.Evaluation.Execution.Tools;
using CoreRentalNet.Evaluation.Execution.Running;

namespace CoreRentalNet.Evaluation.Execution.Auth;

/// <summary>The caller token could not be acquired or read.</summary>
public sealed class CallerTokenException : Exception
{
    public CallerTokenException(string message)
        : base(message)
    {
    }

    public CallerTokenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
