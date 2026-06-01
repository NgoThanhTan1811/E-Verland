using System.Text.Json;


namespace EVerland.Extentions;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request canceled by client: {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "Operation canceled while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (!context.Response.HasStarted)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 499;

                var response = JsonSerializer.Serialize(new
                {
                    error = "Request was canceled",
                    statusCode = 499,
                    timestamp = DateTime.UtcNow
                });

                await context.Response.WriteAsync(response);
            }
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ValidationException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (ConflictException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status401Unauthorized, ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (BadRequestException ex)
        {
            await HandleExceptionAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, "An internal server error occurred");
        }
    }


    private static Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = JsonSerializer.Serialize(new
        {
            error = message,
            statusCode,
            timestamp = DateTime.UtcNow
        });

        return context.Response.WriteAsync(response);
    }
}