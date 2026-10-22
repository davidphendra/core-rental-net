using System.Diagnostics;
using CoreRentalNet.Modules.Catalog.Application.Contracts;
using Microsoft.AspNetCore.Http;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>Records the Host's vector-search time for the current request.</summary>
internal sealed class MeasuredProductSimilarityService(
    IProductSimilarityService inner,
    IHttpContextAccessor httpContextAccessor) : IProductSimilarityService
{
    public async Task<IReadOnlyList<NearestProduct>> NearestAsync(
        float[] queryVector,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await inner.NearestAsync(queryVector, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        httpContextAccessor.HttpContext?.RequestServices
            .GetService<HostRequestTiming>()
            ?.Record("vector-search", stopwatch.Elapsed);

        return result;
    }
}
