using System.Text.Json;


namespace EVerland.Extentions;

public sealed class ApiExceptionExtension(ILogger<ApiExceptionExtension> logger)
{
    private readonly ILogger<ApiExceptionExtension> _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
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