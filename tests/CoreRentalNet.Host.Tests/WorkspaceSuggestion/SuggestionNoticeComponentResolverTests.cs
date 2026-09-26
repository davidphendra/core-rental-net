using AwesomeAssertions;
using CoreRentalNet.Host.Components.Shared.Suggestion;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace CoreRentalNet.Host.Tests.WorkspaceSuggestion;

/// <summary>The registry is complete: every notice kind has a component the renderer can draw.</summary>
/// <remarks>
/// The counterpart to the panel's own <c>switch</c>: a new notice kind with no entry here would draw nothing at
/// all, silently, which is exactly the failure a registry has to be guarded against.
/// </remarks>
public sealed class SuggestionNoticeComponentResolverTests
{
    [Fact]
    public void Every_notice_kind_resolves_to_a_component()
    {
        var resolver = new SuggestionNoticeComponentResolver();
        var noticeKinds = Enum.GetValues<SuggestionNoticeKind>();

        noticeKinds.Should().HaveCountGreaterThanOrEqualTo(5, "the panel has five kinds of notice");

        foreach (var noticeKind in noticeKinds)
        {
            var componentType = resolver.ResolveComponentType(noticeKind);

            typeof(ComponentBase).IsAssignableFrom(componentType).Should().BeTrue(
                $"{noticeKind} must resolve to a component the renderer can draw");
        }
    }
}
