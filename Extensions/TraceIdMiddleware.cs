using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace EVerland.Extentions;

public sealed class TraceIdMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var incoming = context.Request.Headers["traceparent"].FirstOrDefault();
            Activity? activity = Activity.Current;

            if (string.IsNullOrWhiteSpace(incoming))
            {
                activity ??= new Activity("Request");
                activity.Start();
            }
            else
            {
                // Let Activity parse traceparent if present
                activity ??= new Activity("Request");
                try { activity.SetParentId(incoming); } catch { }
                activity.Start();
            }

            var traceId = activity?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
            context.Items["TraceId"] = traceId;
            context.Response.Headers["X-Trace-Id"] = traceId;

            using (LogContext.PushProperty("TraceId", traceId))
            using (LogContext.PushProperty("RequestPath", context.Request.Path))
            using (LogContext.PushProperty("RequestMethod", context.Request.Method))
            {
                await _next(context);
            }
        }
        finally
        {
            // Do not stop Activity here as it may be used by other instrumentation
        }
    }
}
