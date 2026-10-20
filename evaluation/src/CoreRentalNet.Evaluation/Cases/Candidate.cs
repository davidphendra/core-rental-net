namespace CoreRentalNet.Evaluation.Cases;

/// <summary>One approved setup.</summary>
public sealed record Candidate(IReadOnlyList<CandidateLine> Lines);
