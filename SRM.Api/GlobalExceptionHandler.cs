using Microsoft.AspNetCore.Diagnostics;

namespace SRM.Api
{
    public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment env) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

            var (statusCode, title) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied"),
                InvalidOperationException => (StatusCodes.Status400BadRequest, exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            httpContext.Response.StatusCode = statusCode;

            var response = new
            {
                status = statusCode,
                title,
                traceId = httpContext.TraceIdentifier,
                detail = env.IsDevelopment() ? exception.ToString() : null
            };

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            return true;
        }
    }
}
