using Amazon.Runtime;

namespace Infra.AWS.Resilience;

internal static class AwsRetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action, int maxAttempts = 3, CancellationToken ct = default)
    {
        if (maxAttempts < 1)
        {
            maxAttempts = 1;
        }

        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await action();
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                lastException = ex;
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 200);
                await Task.Delay(delay, ct);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        throw lastException ?? new InvalidOperationException("AWS operation failed.");
    }

    public static async Task ExecuteAsync(Func<Task> action, int maxAttempts = 3, CancellationToken ct = default)
    {
        await ExecuteAsync(async () =>
        {
            await action();
            return true;
        }, maxAttempts, ct);
    }

    private static bool IsTransient(Exception exception)
    {
        if (exception is TaskCanceledException)
        {
            return false;
        }

        if (exception is AmazonServiceException amazonException)
        {
            var statusCode = (int)amazonException.StatusCode;
            if (statusCode >= 500)
            {
                return true;
            }

            return amazonException.ErrorCode is "RequestTimeout" or "Throttling" or "ThrottlingException" or "TooManyRequestsException";
        }

        return exception is HttpRequestException or IOException or TimeoutException;
    }
}
