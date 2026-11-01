using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>One approved setup.</summary>
public sealed record Candidate(IReadOnlyList<CandidateLine> Lines);
