namespace CoreRentalNet.Agents.Tests;

/// <summary>One metric point a listener saw, named and tagged as the instrument emitted it.</summary>
internal sealed record RecordedMeasurement(
    string Name,
    double Value,
    KeyValuePair<string, object?>[] Tags);
