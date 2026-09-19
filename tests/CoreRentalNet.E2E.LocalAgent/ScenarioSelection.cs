namespace CoreRentalNet.E2E.LocalAgent;

/// <summary>Which scenario the stand-in is acting.</summary>
/// <remarks>
/// Held by the container rather than in a field, because the stand-in outlives each test and the suite runs
/// some of them at once: a static would make one test's choice another test's answer, and the failure would
/// look like flakiness rather than like shared state.
/// </remarks>
internal sealed class ScenarioSelection
{
    private readonly Lock gate = new();

    private string current = "suggested";

    public string Current
    {
        get
        {
            lock (gate)
            {
                return current;
            }
        }
    }

    /// <summary>Switches the scenario, refusing a name that has no body behind it.</summary>
    public bool Choose(string name)
    {
        if (!ScenarioLibrary.Names.Contains(name, StringComparer.Ordinal))
        {
            return false;
        }

        lock (gate)
        {
            current = name;
        }

        return true;
    }
}
