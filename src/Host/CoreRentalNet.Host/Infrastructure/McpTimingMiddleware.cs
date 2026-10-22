using System.Diagnostics;
using CoreRentalNet.Host.Configs;

namespace CoreRentalNet.Host.Infrastructure;

/// <summary>Adds a Server-Timing header to MCP responses.</summary>
/// <remarks>
/// It is mounted before authentication and mapping, so the total includes authentication and tool execution.
/// The agent reads the header on the MCP response and keeps it as evidence for the evaluation report.
/// </remarks>
internal sealed class McpTimingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, HostRequestTiming timing)
    {
        if (!string.Equals(context.Request.Path.Value, McpRoutes.Path, StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Server-Timing"] = timing.Header(stopwatch.Elapsed);
            return Task.CompletedTask;
        });

        await next(context);
    }
}
