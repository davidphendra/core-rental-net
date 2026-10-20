namespace CoreRentalNet.Evaluation.Cases;

/// <summary>All approved setups for one run.</summary>
public sealed record CandidateSet(IReadOnlyList<Candidate> Candidates)
{
    public static CandidateSet Empty { get; } = new([]);
}
