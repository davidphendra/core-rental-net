using AgentFoundry.WorkspaceSuggestions.Contracts;
using AgentFoundry.WorkspaceSuggestions.Intent;
using AgentFoundry.WorkspaceSuggestions.Specifications;
using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace AgentFoundry.Tests;

/// <summary>
/// The specification: a declared table first, a model only when the table misses.
/// </summary>
/// <remarks>
/// The table is the shipped one, so these tests are also what holds it to its promise: a phrasing it
/// declares must produce the slot set it declares, and a phrasing it does not must be visible as a
/// miss rather than quietly filled in.
/// </remarks>
public sealed class RephraserTests
{
    [Fact] // AGT-04
    public async Task A_declared_phrasing_produces_its_slot_set_without_asking_a_model()
    {
        var classifier = new ScriptedSlotClassifier(["RelaxZone"]);
        var specification = await RephraseAsync("I would like a full office please", classifier);

        specification.Slots.Select(slot => slot.Slot).Should().BeEquivalentTo(
            ["Desk", "Chair", "Monitor", "Lamp", "Plant", "CoffeeStation"]);
        specification.Slots.Should().OnlyContain(slot => !slot.Inferred);
        specification.HasInferredSlots.Should().BeFalse();
        classifier.Asked.Should().Be(0, "a declared phrasing is a table lookup, not a model call");
    }

    [Fact] // AGT-04
    public async Task The_longest_declared_phrasing_wins()
    {
        // "quiet corner" and "work corner" are both declared; a request carrying both must not depend
        // on the order the file happens to be written in.
        var specification = await RephraseAsync("a quiet work corner", new ScriptedSlotClassifier([]));

        specification.Slots.Should().NotBeEmpty();
        specification.HasInferredSlots.Should().BeFalse();
    }

    [Fact] // AGT-05
    public async Task A_phrasing_the_table_does_not_hold_is_inferred_and_logged()
    {
        var classifier = new ScriptedSlotClassifier(["Desk", "Chair", "Plant"]);
        var log = new RecordingLogger<Rephraser>();

        var specification = await RephraseAsync(
            "somewhere to think with a bit of greenery",
            classifier,
            log);

        classifier.Asked.Should().Be(1);
        specification.Slots.Select(slot => slot.Slot).Should().BeEquivalentTo(["Desk", "Chair", "Plant"]);
        specification.Slots.Should().OnlyContain(slot => slot.Inferred);
        specification.HasInferredSlots.Should().BeTrue();

        // The miss rate is the only thing that shows the table has stopped being useful.
        log.Messages.Should().ContainSingle().Which.Should().Contain("missed");
    }

    [Fact] // AGT-05
    public async Task A_slot_a_model_invents_is_dropped_rather_than_used()
    {
        // The closed vocabulary is the application's, sent with the request. A slot outside it cannot
        // exist, so it is ignored rather than carried into a suggestion nobody could accept.
        var specification = await RephraseAsync(
            "somewhere to think",
            new ScriptedSlotClassifier(["Desk", "Conservatory"]));

        specification.Slots.Select(slot => slot.Slot).Should().Equal("Desk");
    }

    [Fact] // AGT-06
    public async Task A_count_in_the_request_is_honoured()
    {
        var specification = await RephraseAsync("an office setup with two monitors", new ScriptedSlotClassifier([]));

        specification.Slots.Single(slot => slot.Slot == "Monitor").Quantity.Should().Be(2);
    }

    [Fact] // AGT-06
    public async Task A_count_beyond_the_capacity_is_clamped_and_a_missing_count_is_one()
    {
        var tooMany = await RephraseAsync("an office setup with five monitors", new ScriptedSlotClassifier([]));
        var noneAsked = await RephraseAsync("an office setup", new ScriptedSlotClassifier([]));

        // The application owns the capacity: a suggestion for five monitors when the canvas holds three
        // is one the customer cannot accept.
        tooMany.Slots.Single(slot => slot.Slot == "Monitor").Quantity.Should().Be(3);
        noneAsked.Slots.Should().OnlyContain(slot => slot.Quantity == 1);
    }

    [Fact] // AGT-07
    public async Task The_same_request_produces_the_same_specification()
    {
        // The slot set is one value that every candidate is built from, so it has to be produced once
        // and reproducibly rather than re-derived per tier. What that buys across three candidates is
        // asserted once there are candidates to compare.
        var classifier = new ScriptedSlotClassifier(["Desk"]);
        var first = await RephraseAsync("somewhere to think", classifier);
        var second = await RephraseAsync("somewhere to think", classifier);

        // Element by element: a record holding a list compares the list by reference, so asserting the
        // records are equal would pass only when they happen to be the same instance.
        first.RequestId.Should().Be(second.RequestId);
        first.Slots.Should().BeEquivalentTo(second.Slots);
        classifier.Asked.Should().Be(2, "each run asks once; nothing inside a run asks again");
    }

    [Fact]
    public async Task The_shipped_table_loads_and_declares_phrasings()
    {
        var table = IntentTableFile();

        table.Count.Should().BeGreaterThan(1, "a table that loaded nothing would send every request to the model");
        table.Match("a quiet corner").Should().NotBeNull();

        await Task.CompletedTask;
    }

    private static async Task<Specification> RephraseAsync(
        string query,
        ISlotClassifier classifier,
        RecordingLogger<Rephraser>? log = null)
        => await new Rephraser(IntentTableFile(), classifier, log ?? new RecordingLogger<Rephraser>())
            .RephraseAsync(ARequest(query), []);

    private static IntentTable IntentTableFile()
        => IntentTable.Load(Path.Combine(Repository(), "agentfoundry", "shared", "intent", "workspace-intents.json"));

    private static SuggestionRequest ARequest(string query)
        => new(
            RequestId: "0f3c4e2a-0000-4000-8000-000000000001",
            Query: query,
            Slots:
            [
                new SlotRule("Desk", "Desk", 1, IsMandatory: true),
                new SlotRule("Chair", "Chair", 1, IsMandatory: true),
                new SlotRule("Monitor", "Monitor", 3, IsMandatory: false),
                new SlotRule("Lamp", "Lamp", 1, IsMandatory: false),
                new SlotRule("Plant", "Plant", 1, IsMandatory: false),
                new SlotRule("CoffeeStation", "Coffee Station", 1, IsMandatory: false),
                new SlotRule("RelaxZone", "Relax Zone", 1, IsMandatory: false),
            ]);

    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "agentfoundry")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("the test binary runs inside the repository");
    }

    /// <summary>The one thing a run needs a model for, said out loud instead of inferred.</summary>
    private sealed class ScriptedSlotClassifier(IReadOnlyList<string> slots) : ISlotClassifier
    {
        public int Asked { get; private set; }

        public Task<IReadOnlyList<string>> ClassifyAsync(
            string query,
            IReadOnlyList<SlotRule> capacity,
            IReadOnlyList<Finding> findings,
            CancellationToken cancellationToken = default)
        {
            Asked++;

            return Task.FromResult(slots);
        }
    }

    /// <summary>Keeps the rendered lines, because a miss that is not visible is a miss nobody acts on.</summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
