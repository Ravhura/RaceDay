using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Middleware;

/// <summary>
/// Turns exceptions into one consistent JSON error shape (ProblemDetails).
/// Stack traces, SQL errors and other internals are never sent to the caller.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int status;
        string detail;

        switch (exception)
        {
            case AppException app:
                status = app.StatusCode;
                detail = app.Message;
                break;

            case DbUpdateException:
                // Safety net: a database constraint (e.g. unique email / unique enrolment) was hit.
                _logger.LogWarning(exception, "Database constraint violation.");
                status = StatusCodes.Status409Conflict;
                detail = "The request conflicts with data that already exists.";
                break;

            default:
                _logger.LogError(exception, "Unhandled exception.");
                status = StatusCodes.Status500InternalServerError;
                detail = "An unexpected error occurred.";
                break;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = detail
        }, cancellationToken);

        return true;
    }

    private static string TitleFor(int status) => status switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        _ => "Server Error"
    };
}