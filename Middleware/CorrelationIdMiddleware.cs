using System.Diagnostics;
using Serilog.Context;

namespace GVC.Web.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = ObterCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string ObterCorrelationId(HttpContext context)
    {
        string recebido = context.Request.Headers[HeaderName].FirstOrDefault()?.Trim() ?? string.Empty;
        if (recebido.Length is > 0 and <= 128 && recebido.All(x => char.IsLetterOrDigit(x) || x is '-' or '_' or '.'))
            return recebido;

        return Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
    }
}
