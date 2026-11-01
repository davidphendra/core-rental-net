using System.Text.RegularExpressions;
using AwesomeAssertions;
using CoreRentalNet.Evaluation.Configuration;
using CoreRentalNet.Evaluation.Evaluation.Pipeline;
using CoreRentalNet.Evaluation.Evaluation.Checks;
using CoreRentalNet.Evaluation.Evaluation.Judging;
using CoreRentalNet.Evaluation.Evaluation.Timing;
using CoreRentalNet.Evaluation.Evaluation.Stochastic;
using Xunit;

using CoreRentalNet.Evaluation.Tests.Fixtures;

namespace CoreRentalNet.Evaluation.Tests.Judges;

public sealed class AgentEvalAdoptionTests
{
    private static readonly Regex LocalRagMetric = new(
        @"\bclass\s+(Faithfulness|AnswerCorrectness|ContextPrecision|ContextRecall)Metric\b",
        RegexOptions.Compiled);

    [Fact]
    public void The_evaluation_project_pins_agent_eval()
    {
        var project = File.ReadAllText(Path.Combine(
            EvaluationRepoRoot.Path,
            "evaluation",
            "src",
            "CoreRentalNet.Evaluation",
            "CoreRentalNet.Evaluation.csproj"));

        project.Should().Contain("<PackageReference Include=\"AgentEval\" Version=\"1.0.0-rc.1\" />");
    }

    [Fact]
    public void The_policy_digest_distinguishes_the_judge_modes()
    {
        EvaluationPolicy.Digest(JudgeMode.Rag)
            .Should().NotBe(EvaluationPolicy.Digest(JudgeMode.Equivalence));
        EvaluationPolicy.Digest(JudgeMode.Rag)
            .Should().NotBe(EvaluationPolicy.Digest(JudgeMode.Both));
    }

    [Fact]
    public void No_local_rag_metric_type_is_declared()
    {
        var source = Path.Combine(
            EvaluationRepoRoot.Path,
            "evaluation",
            "src",
            "CoreRentalNet.Evaluation");

        var files = Directory
            .GetFiles(source, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains("/obj/", StringComparison.Ordinal)
                           && !file.Contains("/bin/", StringComparison.Ordinal))
            .ToArray();

        files.Should().NotBeEmpty("the guard would be vacuous without the evaluation source");

        var offenders = files
            .SelectMany(file => File.ReadAllLines(file).Select(line => (File: file, Line: line)))
            .Where(entry => LocalRagMetric.IsMatch(entry.Line))
            .Select(entry => $"{entry.File}: {entry.Line.Trim()}")
            .ToArray();

        offenders.Should().BeEmpty("AgentEval owns the RAG metric types");
    }
}
