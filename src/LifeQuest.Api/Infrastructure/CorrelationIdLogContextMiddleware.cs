using System.Diagnostics;
using Gvn.GvnFramework.AspNetCore.Middleware;
using Serilog.Context;

namespace LifeQuest.Api.Infrastructure;

/// <summary>
/// Framework <see cref="CorrelationIdMiddleware"/> kimliği yalnızca HttpContext.Items'a yazar; log'lara
/// taşımaz. Bu middleware kimliği temizler (istemciden gelen değer log enjeksiyonuna açık olmasın),
/// Serilog LogContext'e ve aktif trace'e ekler.
/// </summary>
internal sealed class CorrelationIdLogContextMiddleware(RequestDelegate next)
{
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var raw = context.Items[CorrelationIdMiddleware.CorrelationIdHeader] as string;
        var correlationId = Sanitize(raw) ?? Guid.NewGuid().ToString("N")[..16];

        context.Items[CorrelationIdMiddleware.CorrelationIdHeader] = correlationId;
        context.Response.Headers[CorrelationIdMiddleware.CorrelationIdHeader] = correlationId;
        Activity.Current?.SetTag("correlation_id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }

    private static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = new string(value.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').Take(MaxLength).ToArray());
        return cleaned.Length == 0 ? null : cleaned;
    }
}
