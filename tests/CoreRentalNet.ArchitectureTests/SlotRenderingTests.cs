using System.Text.RegularExpressions;
using AwesomeAssertions;
using CoreRentalNet.Modules.Workspace.Domain;
using Xunit;

namespace CoreRentalNet.ArchitectureTests;

/// <summary>
/// Source-level checks on the canvas. These exist because a slot with styling but no component
/// renders as nothing at all — which is exactly what happened once, and no unit test noticed.
/// </summary>
public sealed class SlotRenderingTests
{
    private static string Shared(string file) => Path.Combine(RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "Components", "Shared"), file);

    private static string Design => File.ReadAllText(RepoRoot.Combine("specs", "design", "interactive_builder", "code.html"));

    private static string Generated => File.ReadAllText(RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "wwwroot", "styles", "tailwind.css"));

    /// <summary>The five slots the design draws on the stage, as opposed to the two it lays out below.</summary>
    private static readonly SlotId[] StageSlots = [SlotId.Desk, SlotId.Chair, SlotId.Monitor, SlotId.Lamp, SlotId.Plant];

    /// <summary>
    /// The one slot whose positioning the design does not get to decide, and why.
    /// </summary>
    /// <remarks>
    /// The design draws the chair tucked under the desk: 64px below it and 160px tall, which covers
    /// the desk's own 32px bar - the only place a desk can be clicked from, so the desk could not be
    /// added at all. At the owner's request it now stops at the desk's baseline, which is one
    /// chair-height down, and is the lamp's size. Its classes are still checked for a rule in the
    /// generated stylesheet, so a class that reaches the browser doing nothing still fails here.
    /// </remarks>
    private static readonly Dictionary<SlotId, string[]> PositionedDeliberately = new()
    {
        [SlotId.Chair] = ["-bottom-24"],
    };

    /// <summary>
    /// The positions SlotCss names, read out of the file that names them. Reading the source rather
    /// than calling the method keeps this test free of a reference to the application.
    /// </summary>
    private static Dictionary<SlotId, string> FromSlotCss(string method)
    {
        var source = File.ReadAllText(RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "Presentation", "SlotCss.cs"));
        var start = source.IndexOf($" {method}(", StringComparison.Ordinal);
        var end = source.IndexOf("\n    public static", start + 1, StringComparison.Ordinal);
        var body = end < 0 ? source[start..] : source[start..end];
        var found = new Dictionary<SlotId, string>();

        foreach (var pair in Regex.Matches(body, @"SlotId\.(\w+)\s*=>\s*""([^""]*)""").Cast<Match>())
        {
            found[Enum.Parse<SlotId>(pair.Groups[1].Value)] = pair.Groups[2].Value;
        }

        return found;
    }

    [Fact] // UI-02
    public void Every_slot_is_rendered_by_the_canvas_or_by_the_zone_list()
    {
        var source = File.ReadAllText(Shared("SlotCanvas.razor")) + File.ReadAllText(Shared("ZoneList.razor"));

        foreach (var slot in Enum.GetValues<SlotId>())
        {
            source.Should().Contain(
                $"SlotId.{slot}",
                $"slot {slot} is never rendered, so it would be invisible on the canvas");
        }
    }

    [Fact] // UI-02
    public void Every_slot_carries_the_class_the_browser_tests_look_for()
    {
        var classes = FromSlotCss("ClassFor");

        classes.Should().HaveCount(Enum.GetValues<SlotId>().Length, "every slot needs the class the browser tests look for");

        foreach (var slot in Enum.GetValues<SlotId>())
        {
            classes.Should().ContainKey(slot);
            classes[slot].Should().Be($"slot--{Kebab(slot)}");
        }
    }

    /// <summary>
    /// A slot's position is not invented: it is the string the design uses for that slot. Both
    /// halves matter — that the design still says it, and that the generated stylesheet still has
    /// a rule for it, since a class that reaches the browser without a rule lays the stage out by
    /// accident.
    /// </summary>
    [Fact] // UI-02
    public void Every_slot_that_sits_on_the_stage_is_positioned_the_way_the_design_positions_it()
    {
        var positions = FromSlotCss("StagePositionFor");

        positions.Keys.Should().BeEquivalentTo(StageSlots, "the stage holds five slots and the zones are laid out, not positioned");

        foreach (var slot in StageSlots)
        {
            var tokens = positions[slot].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            tokens.Should().NotBeEmpty($"slot {slot} is on the stage and needs a position");

            foreach (var token in tokens)
            {
                var deliberate = PositionedDeliberately.TryGetValue(slot, out var allowed) && allowed.Contains(token);

                if (!deliberate)
                {
                    Design.Should().Contain(token, $"slot {slot} is positioned with '{token}', which the design does not use");
                }

                Generated.Should().Contain(CssSelector(token), $"'{token}' has no rule in the generated stylesheet, so it does nothing");
            }
        }
    }

    [Fact] // UI-02
    public void A_zone_is_laid_out_by_the_grid_and_has_no_position_of_its_own()
    {
        foreach (var zone in new[] { SlotId.CoffeeStation, SlotId.RelaxZone })
        {
            var position = () => FromSlotCss("StagePositionFor")[zone];

            position.Should().Throw<KeyNotFoundException>($"{zone} is a zone: the grid places it, so giving it a coordinate would be a second answer");
        }
    }

    [Fact] // UI-02
    public void Slot_geometry_is_named_in_one_place_and_not_drawn_in_component_code()
    {
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(Shared("SlotCanvas.razor"))!, "Slot*.razor"))
        {
            var text = File.ReadAllText(file);

            text.Should().NotContain("--slot-x", $"{Path.GetFileName(file)} must not hold a coordinate; SlotCss holds the position");
            text.Should().NotContain("style=\"left", $"{Path.GetFileName(file)} must not position a slot by hand");
        }
    }

    /// <summary>Tailwind escapes the punctuation in a class name when it writes the selector.</summary>
    private static string CssSelector(string className)
    {
        foreach (var character in "/[].:")
        {
            className = className.Replace(character.ToString(), "\\" + character, StringComparison.Ordinal);
        }

        return "." + className;
    }

    private static string Kebab(SlotId slot)
    {
        var name = slot.ToString();
        var builder = new System.Text.StringBuilder(name.Length + 4);

        foreach (var character in name)
        {
            if (char.IsUpper(character) && builder.Length > 0)
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
