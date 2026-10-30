namespace CoreRentalNet.Evaluation.Configuration;

/// <summary>Which judge layers one evaluation run enables.</summary>
public enum JudgeMode
{
    /// <summary>AgentEval RAG metrics only.</summary>
    Rag,

    /// <summary>The domain equivalence judge only.</summary>
    Equivalence,

    /// <summary>Both judge layers.</summary>
    Both,
}
