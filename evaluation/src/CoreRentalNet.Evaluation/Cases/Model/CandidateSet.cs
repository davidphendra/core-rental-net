using CoreRentalNet.Evaluation.Cases.Parsing;
using CoreRentalNet.Evaluation.Cases.Validation;

namespace CoreRentalNet.Evaluation.Cases.Model;

/// <summary>All approved setups for one run.</summary>
public sealed record CandidateSet(IReadOnlyList<Candidate> Candidates)
{
    public static CandidateSet Empty { get; } = new([]);
}
