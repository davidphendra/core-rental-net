using AwesomeAssertions;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace CoreRentalNet.E2E.Flows;

/// <summary>
/// The typography the application ships, checked in the layout engine rather than in the stylesheet.
/// </summary>
/// <remarks>
/// This exists because a declared weight is not a rendered one. Both families once shipped one file
/// per family under three names - Manrope-600 and Manrope-700 were the same bytes, as were all three
/// Plus Jakarta Sans files - so every weight fell back to the same face. Nothing about that is
/// visible in the CSS: `font-weight: 400` computed to 400 and the browser drew the 600 file, which is
/// why a label asking for regular text came out semibold and an emphasised phrase inside it could not
/// be told apart from the words around it.
/// </remarks>
public sealed class TypographyTests(HostFixture host, ITestOutputHelper output) : E2ETest(host, output)
{
    [Theory] // UI-11
    [InlineData("Manrope")]
    [InlineData("Plus Jakarta Sans")]
    public async Task Every_weight_the_application_declares_is_a_face_of_its_own(string family)
    {
        await GotoAsync("/");

        var widths = await Page.EvaluateAsync<double[]>(@"async (family) => {
          const weights = [400, 600, 700];
          const text = 'Type this is a demo to confirm';

          // Each face is loaded before anything is measured: a measurement taken while they are still
          // arriving compares fallbacks, and a fallback changes width when the browser synthesises
          // bold from it, which reads as three real faces and is not.
          for (const weight of weights) {
            await document.fonts.load(`${weight} 16px '${family}'`, text);
          }
          await document.fonts.ready;

          return weights.map(weight => {
            const span = document.createElement('span');
            span.textContent = text;
            span.style.cssText = `position:absolute; visibility:hidden; white-space:nowrap; `
              + `font-family:'${family}'; font-weight:${weight}; font-size:16px`;
            document.body.appendChild(span);
            const width = span.getBoundingClientRect().width;
            span.remove();
            return Math.round(width * 100) / 100;
          });
        }", family);

        widths.Should().HaveCount(3);
        widths.Distinct().Should().HaveCount(
            3,
            $"the same words at 400, 600 and 700 must not come out the same width in {family}: a weight "
            + "with no file of its own renders the nearest one instead");
    }
}
