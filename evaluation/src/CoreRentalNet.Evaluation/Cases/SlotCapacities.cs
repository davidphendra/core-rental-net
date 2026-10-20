namespace CoreRentalNet.Evaluation.Cases;

/// <summary>The capacities the application states for a workspace.</summary>
public sealed record SlotCapacities(
    int Desk,
    int Chair,
    int Monitor,
    int Lamp,
    int Plant,
    int CoffeeStation,
    int RelaxZone)
{
    public static SlotCapacities Default { get; } = new(
        Desk: 1,
        Chair: 1,
        Monitor: 3,
        Lamp: 1,
        Plant: 1,
        CoffeeStation: 1,
        RelaxZone: 1);

    public int For(string slot)
        => slot switch
        {
            EvaluationSlot.Desk => Desk,
            EvaluationSlot.Chair => Chair,
            EvaluationSlot.Monitor => Monitor,
            EvaluationSlot.Lamp => Lamp,
            EvaluationSlot.Plant => Plant,
            EvaluationSlot.CoffeeStation => CoffeeStation,
            EvaluationSlot.RelaxZone => RelaxZone,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown workspace slot."),
        };

    public IReadOnlyDictionary<string, int> AsDictionary()
        => new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [EvaluationSlot.Desk] = Desk,
            [EvaluationSlot.Chair] = Chair,
            [EvaluationSlot.Monitor] = Monitor,
            [EvaluationSlot.Lamp] = Lamp,
            [EvaluationSlot.Plant] = Plant,
            [EvaluationSlot.CoffeeStation] = CoffeeStation,
            [EvaluationSlot.RelaxZone] = RelaxZone,
        };
}
