using System.Net;
using Glyloop.API.Contracts.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Glyloop.API.Middleware;

/// <summary>
/// Global exception handler that catches unhandled exceptions and returns structured error responses.
/// Implements IExceptionHandler (.NET 8+) for improved performance and clarity.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        _logger.LogError(
            exception,
            "An unhandled exception occurred. TraceId: {TraceId}",
            traceId);

        var (statusCode, title, detail) = MapException(exception);

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = _environment.IsDevelopment() ? detail : "An error occurred processing your request.",
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        // Add validation errors if applicable
        if (exception is ArgumentException or ArgumentNullException)
        {
            problemDetails.Extensions["errors"] = new Dictionary<string, string[]>
            {
                ["validation"] = new[] { exception.Message }
            };
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; // Exception handled
    }

    private (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentException or ArgumentNullException => 
                (StatusCodes.Status400BadRequest, 
                 "Bad Request", 
                 exception.Message),

            InvalidOperationException => 
                (StatusCodes.Status400BadRequest, 
                 "Invalid Operation", 
                 exception.Message),

            UnauthorizedAccessException => 
                (StatusCodes.Status401Unauthorized, 
                 "Unauthorized", 
                 "You are not authorized to access this resource."),

            KeyNotFoundException => 
                (StatusCodes.Status404NotFound, 
                 "Not Found", 
                 exception.Message),

            TimeoutException => 
                (StatusCodes.Status408RequestTimeout, 
                 "Request Timeout", 
                 "The request timed out. Please try again."),

            _ => 
                (StatusCodes.Status500InternalServerError, 
                 "Internal Server Error", 
                 exception.Message)
        };
    }
}

