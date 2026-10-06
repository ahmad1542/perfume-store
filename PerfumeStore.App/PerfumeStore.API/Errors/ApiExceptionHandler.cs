using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Domain.Exceptions;

namespace PerfumeStore.API.Errors;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler {
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken) {
        var (status, title, detail) = exception switch {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Business rule violation", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred. Please try again later.")
        };

        if (status == StatusCodes.Status500InternalServerError) {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}. Trace ID: {TraceId}",
                context.Request.Method, context.Request.Path, context.TraceIdentifier);
        }

        context.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext {
            HttpContext = context,
            ProblemDetails = new ProblemDetails {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Extensions = { ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier }
            }
        });
    }
}
