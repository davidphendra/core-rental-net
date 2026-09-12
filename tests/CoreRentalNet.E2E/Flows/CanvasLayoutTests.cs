using AwesomeAssertions;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The canvas's arrangement, which is partly the design's and partly the owner's: which side the lamp
/// stands on, how much room the chair and the monitors have, and how a filled desk carries its own
/// name and price.
/// </summary>
public sealed class CanvasLayoutTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Fact] // LAYOUT-01
    public async Task The_lamp_stands_at_the_left_of_the_desk_and_the_plant_at_the_right()
    {
        await GotoAsync("/builder");

        var sides = await Page.EvaluateAsync<double[]>(@"() => {
          const centre = el => { const r = el.getBoundingClientRect(); return Math.round(r.left + r.width / 2); };
          return [centre(document.querySelector('.scene')),
                  centre(document.querySelector('.slot--lamp')),
                  centre(document.querySelector('.slot--plant'))];
        }");

        sides[1].Should().BeLessThan(sides[0], "the lamp stands at the left of the canvas");
        sides[2].Should().BeGreaterThan(sides[0], "the plant stands at the right");
    }

    [Fact] // LAYOUT-02
    public async Task The_chair_hangs_clear_of_the_desk_and_of_the_bottom_of_the_stage()
    {
        await GotoAsync("/builder");

        var gaps = await Page.EvaluateAsync<double[]>(@"() => {
          const desk = document.querySelector('.slot--desk').getBoundingClientRect();
          const chair = document.querySelector('.slot--chair').getBoundingClientRect();
          const stage = document.querySelector('.workspace-stage').getBoundingClientRect();
          return [Math.round(chair.top - desk.bottom), Math.round(stage.bottom - chair.bottom)];
        }");

        gaps[0].Should().BeGreaterThanOrEqualTo(16, "the chair does not touch the desk");
        gaps[1].Should().BeGreaterThanOrEqualTo(40, "and it does not touch the bottom of the stage");
    }

    [Fact] // LAYOUT-03
    public async Task The_monitors_are_small_and_the_row_stands_clear_of_the_desk()
    {
        await GotoAsync("/builder");

        var measured = await Page.EvaluateAsync<double[]>(@"() => {
          const boxes = [...document.querySelectorAll('.slot--monitor')].map(el => el.getBoundingClientRect());
          const desk = document.querySelector('.slot--desk').getBoundingClientRect();
          return [Math.round(boxes[0].width), Math.round(boxes[0].height),
                  Math.round(desk.top - Math.max(...boxes.map(b => b.bottom)))];
        }");

        measured[0].Should().BeLessThan(192, "smaller than the box the design gives a monitor");
        measured[1].Should().BeLessThan(128);
        measured[2].Should().BeGreaterThanOrEqualTo(112, "with room between the row and the desk below it");
    }

    [Fact] // LAYOUT-04
    public async Task A_filled_desk_centres_its_name_and_puts_its_price_at_the_right()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/builder");

        var measured = await Page.EvaluateAsync<double[]>(@"() => {
          const desk = document.querySelector('.slot--desk').getBoundingClientRect();
          const name = document.querySelector('.slot--desk .slot__name').getBoundingClientRect();
          const price = document.querySelector('.slot--desk .slot__price').getBoundingClientRect();
          const picture = document.querySelector('.slot--desk .slot__media').getBoundingClientRect();
          return [Math.round((name.left + name.width / 2) - (desk.left + desk.width / 2)),
                  Math.round(desk.right - price.right),
                  Math.round(picture.width)];
        }");

        // The name is not centred on what is left of the desk once the picture and the price have
        // taken their share, which is what it used to be: it sat 100px right of the desk's middle.
        measured[0].Should().BeInRange(-1, 1, "the name is centred on the desk");
        measured[1].Should().BeInRange(0, 12, "the price sits at the desk's right end");
        measured[2].Should().BeLessThanOrEqualTo(32, "and the desk's picture is a mark at the left, not a band behind the name");
    }

    [Fact] // LAYOUT-05
    public async Task The_picker_leaves_room_under_its_search_field()
    {
        await GotoAsync("/builder");
        await OpenSlotPickerAsync(".slot--chair");

        var gap = await Page.EvaluateAsync<double>(
            @"() => {
                const field = document.querySelector('#picker-search').getBoundingClientRect();
                const list = document.querySelector('dialog[open] .picker').getBoundingClientRect();
                return Math.round(list.top - field.bottom);
              }");

        gap.Should().BeGreaterThanOrEqualTo(20, "the products do not start against the search field");
    }

    [Fact] // LAYOUT-06
    public async Task The_lamp_and_the_plant_stand_clear_of_the_row_above_them()
    {
        await GotoAsync("/builder");

        var gaps = await Page.EvaluateAsync<double[]>(@"() => {
          const rect = el => el.getBoundingClientRect();
          const rowBottom = Math.max(...[...document.querySelectorAll('.slot--monitor')].map(m => rect(m).bottom));
          const desk = rect(document.querySelector('.slot--desk'));
          return [...['.slot--lamp', '.slot--plant']].flatMap(selector => {
            if (typeof selector !== 'string') { return []; }
            const card = rect(document.querySelector(selector));
            return [Math.round(card.top - rowBottom), Math.round(desk.top - card.bottom)];
          });
        }");

        // The cards used to begin on exactly the line the monitor row ended on.
        gaps.Should().HaveCount(4);
        gaps[0].Should().BeGreaterThanOrEqualTo(16, "the lamp clears the row above it");
        gaps[1].Should().BeGreaterThanOrEqualTo(16, "and the desk below it");
        gaps[2].Should().BeGreaterThanOrEqualTo(16, "the plant clears the row above it");
        gaps[3].Should().BeGreaterThanOrEqualTo(16, "and the desk below it");
    }
}
