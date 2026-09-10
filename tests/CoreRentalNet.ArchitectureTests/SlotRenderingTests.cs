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

    private static string Styles => File.ReadAllText(RepoRoot.Combine("src", "Host", "CoreRentalNet.Host", "wwwroot", "styles", "slots.css"));

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
    public void Every_slot_that_sits_on_the_canvas_has_geometry()
    {
        foreach (var slot in new[] { SlotId.Desk, SlotId.Chair, SlotId.Monitor, SlotId.Lamp, SlotId.Plant })
        {
            var cssClass = $".slot--{Kebab(slot)}";

            Styles.Should().Contain(cssClass, $"slot {slot} is on the canvas and needs a position");
        }

        // The scene needs an x and a y for every positioned slot, declared as custom properties.
        foreach (var slot in new[] { SlotId.Desk, SlotId.Chair, SlotId.Monitor, SlotId.Lamp, SlotId.Plant })
        {
            var block = Styles[Styles.IndexOf($".slot--{Kebab(slot)}", StringComparison.Ordinal)..];
            var end = block.IndexOf('}');

            block[..end].Should().Contain("--slot-x").And.Contain("--slot-y");
        }
    }

    [Fact] // UI-02
    public void Slot_geometry_is_declared_in_css_and_not_in_component_code()
    {
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(Shared("SlotCanvas.razor"))!, "Slot*.razor"))
        {
            var text = File.ReadAllText(file);

            text.Should().NotContain("left:", $"{Path.GetFileName(file)} must not position a slot; slots.css does that");
            text.Should().NotContain("position: absolute", $"{Path.GetFileName(file)} must not position a slot");
        }
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
