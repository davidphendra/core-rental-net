using System.Diagnostics;
using CoreRentalNet.BuildingBlocks.Application.Embeddings;
using Microsoft.AspNetCore.Http;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>Records the Host's embedding time for the current request.</summary>
internal sealed class MeasuredEmbeddingService(
    IEmbeddingService inner,
    IHttpContextAccessor httpContextAccessor) : IEmbeddingService
{
    public async Task<IReadOnlyList<float[]>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await inner.EmbedAsync(texts, cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        httpContextAccessor.HttpContext?.RequestServices
            .GetService<HostRequestTiming>()
            ?.Record("embedding", stopwatch.Elapsed);

        return result;
    }
}
