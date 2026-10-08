using Microsoft.Agents.AI.Workflows;
using CoreRentalNet.Agents.Features.EchoReverse.Nodes;

namespace CoreRentalNet.Agents.Features.EchoReverse;

internal static class EchoWorkflowFactory
{
    private const string Name = "echo-reverse-workflow";

    public static Workflow Build()
    {
        var echoReverseNode = new ReverseNodeExecutor()
            .BindExecutor();

        return new WorkflowBuilder(echoReverseNode)
            .WithOutputFrom(echoReverseNode)
            .WithName(Name)
            .Build();
    }
}
