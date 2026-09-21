using CoreRentalNet.Modules.Discovery.Application.Shortlist;

namespace CoreRentalNet.Host.Tests;

/// <summary>A search that never answers, so a test can watch the run's own deadline end it.</summary>
/// <remarks>
/// It waits on the token rather than sleeping: a real sleep would make the test slow and would still pass if the
/// budget were never applied, because the run would simply finish late. This can only return by being cancelled,
/// which is precisely the case e06s03 task 2 exists for — the embedding hop was outside the budget until then.
/// </remarks>
internal sealed class HangingCatalogShortlist : ICatalogShortlist
{
    public async Task<IReadOnlyList<ShortlistItem>> ForAsync(string query, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);

        return [];
    }
}
