using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace SharedKernel.Context;

public sealed class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpRequestContext(IHttpContextAccessor accessor) => _accessor = accessor;

    public string TraceId =>
        _accessor.HttpContext?.Items["TraceId"] as string
        ?? Activity.Current?.TraceId.ToString()
        ?? Guid.NewGuid().ToString("N");
}
