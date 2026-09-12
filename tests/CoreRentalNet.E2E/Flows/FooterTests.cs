using AwesomeAssertions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The version at the foot of every page, and how the pages whose bottom edge is already taken
/// share it.
/// </summary>
/// <remarks>
/// What the version <em>is</em> is checked in CoreRentalNet.Host.Tests, which can see the display's
/// rule; this project starts the application as a process and reads what it renders, so it asks
/// only that a version is there and that it looks like one.
/// </remarks>
public sealed class FooterTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    private const string FooterGeometry = @"() => {
      const r = document.querySelector('footer.app-footer').getBoundingClientRect();
      const version = document.querySelector('.app-footer__version').getBoundingClientRect();
      return [Math.round(r.left), Math.round(r.right), Math.round(r.top), Math.round(r.bottom),
              Math.round(version.left + version.width / 2), window.innerWidth, window.innerHeight];
    }";

    private async Task ShowAVersionAsync()
    {
        var shown = await Page.Locator(".app-footer__version").TextContentAsync();

        shown.Should().MatchRegex(@"^\d+\.\d+\.\d+$", "the footer shows a version, and only a version");
    }

    [Theory] // FOOT-01
    [InlineData("/")]
    [InlineData("/builder")]
    [InlineData("/extras")]
    public async Task Every_page_ends_with_the_version(string path)
    {
        await GotoAsync(path);

        var footer = Page.Locator("footer.app-footer");
        await Expect(footer).ToBeVisibleAsync();
        await ShowAVersionAsync();

        var page_ = await footer.EvaluateAsync<double[]>(FooterGeometry);

        // It is at the foot of the document, which for a page shorter than the window is the bottom
        // of the window, and it spans the window rather than sitting in a column.
        page_[3].Should().BeGreaterThanOrEqualTo(page_[6] - 1, "the footer reaches the bottom of the window");
        page_[0].Should().Be(0);
        page_[1].Should().Be(page_[5], "the footer spans the window");
    }

    [Fact] // FOOT-02
    public async Task The_page_that_summarises_the_rental_ends_with_the_version_too()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/review");

        await Expect(Page.Locator("footer.app-footer")).ToBeVisibleAsync();
        await ShowAVersionAsync();
    }

    [Theory] // FOOT-03
    [InlineData("/")]
    [InlineData("/builder")]
    [InlineData("/extras")]
    public async Task The_version_is_centred_on_the_window_on_every_page(string path)
    {
        await GotoAsync(path);

        var page_ = await Page.Locator("footer.app-footer").EvaluateAsync<double[]>(FooterGeometry);

        // Including the builder, whose panel covers the left of the window: the panel stops above
        // the footer rather than beside it, so there is nothing there to centre around.
        (page_[4] - page_[5] / 2).Should().BeInRange(-1, 1, "the version is centred on the window");
    }

    [Fact] // FOOT-04
    public async Task On_the_builder_the_panel_reaches_the_foot_of_the_window_and_the_footer_passes_behind_it()
    {
        await AssignADeskAndAChairAsync();
        await GotoAsync("/builder");

        // Scrolled by script rather than by a click, because clicking scrolls the target into view
        // first and would measure a position the page never rests at.
        await Page.EvaluateAsync("window.scrollTo(0, document.documentElement.scrollHeight)");
        await Task.Delay(300);

        var geometry = await Page.EvaluateAsync<double[]>(@"() => {
          const pill = document.querySelector('.total-bar').getBoundingClientRect();
          const footer = document.querySelector('footer.app-footer').getBoundingClientRect();
          const version = document.querySelector('.app-footer__version').getBoundingClientRect();
          const panel = document.querySelector('aside').getBoundingClientRect();
          const corner = document.elementFromPoint(160, window.innerHeight - 20);
          return [Math.round(footer.top - pill.bottom),
                  Math.round(pill.left - panel.right),
                  Math.round(panel.bottom - window.innerHeight),
                  Math.round(version.left - panel.right),
                  corner && corner.closest('aside') ? 1 : 0];
        }");

        geometry[0].Should().Be(32, "the pill keeps the design's 32px gap, measured from the footer rather than the window");
        geometry[1].Should().BeGreaterThanOrEqualTo(0, "the pill floats over the content column, clear of the panel");

        // The panel keeps the height its design gives it, so it runs to the foot of the window and
        // leaves no strip of page showing beside the footer: the footer scrolls behind it, and the
        // version is far enough right to stay clear.
        geometry[2].Should().Be(0, "the panel reaches the foot of the window");
        geometry[3].Should().BeGreaterThanOrEqualTo(0, "the version is clear of the panel, so it is never covered");
        geometry[4].Should().Be(1, "the bottom-left corner belongs to the panel, not to the page scrolling past");
    }

    [Fact] // FOOT-05
    public async Task On_a_phone_the_version_is_not_underneath_the_workspace_nav()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await GotoAsync("/");

        // The home page is taller than a phone, so the footer is below the fold until the page is
        // scrolled: measured before that, its document position says nothing about the fixed nav,
        // which never moves with the page.
        await Page.EvaluateAsync("window.scrollTo(0, document.documentElement.scrollHeight)");
        await Task.Delay(300);

        var geometry = await Page.EvaluateAsync<double[]>(@"() => {
          const footer = document.querySelector('footer.app-footer').getBoundingClientRect();
          const nav = document.querySelector('.bottom-nav').getBoundingClientRect();
          return [Math.round(footer.bottom), Math.round(nav.top)];
        }");

        geometry[0].Should().BeLessThanOrEqualTo(geometry[1], "the fixed workspace nav would cover the version");
    }

    [Fact] // FOOT-06
    public async Task The_footer_is_made_of_the_headers_own_surface_and_palette()
    {
        await GotoAsync("/");

        var styles = await Page.EvaluateAsync<string[]>(@"() => {
          const of = el => getComputedStyle(el);
          const header = of(document.querySelector('.app-header'));
          const link = of(document.querySelector('.app-header__link:not([aria-current])'));
          const footer = of(document.querySelector('.app-footer'));
          const version = of(document.querySelector('.app-footer__version'));
          return [header.backgroundColor, footer.backgroundColor,
                  link.color, version.color,
                  link.fontFamily, version.fontFamily];
        }");

        // The two ends of the page are one design rather than two: same surface, same muted
        // palette, same label face. The link compared against is one that is not the current page,
        // because the header colours the current one with the brand's teal. The size differs on
        // purpose - a footer is fine print rather than navigation - and is not asserted here.
        styles[0].Should().Be(styles[1], "the footer's surface is the header's");
        styles[2].Should().Be(styles[3], "its text is the muted colour the header's links use");
        styles[4].Should().Be(styles[5], "and the same label face");

        // Nothing separates it from the page above: no rule, no different surface. The footer is
        // the page's end rather than a band across it.
        var border = await Page.Locator("footer.app-footer")
            .EvaluateAsync<double>("f => parseFloat(getComputedStyle(f).borderTopWidth)");

        border.Should().Be(0, "the footer is seamless with the page above it");
    }
}
