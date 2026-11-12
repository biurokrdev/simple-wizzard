using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace D2ServicesCreator.Api.Middleware;

/// <summary>
/// Global exception handler returning RFC 7807 <see cref="ProblemDetails"/>. Internal messages and
/// stack traces are never written to the response — external systems get a generic 500 and a
/// <c>traceId</c> to quote when reporting the problem; details stay in the logs.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationException => HandleValidationException(validationException),
            ArgumentException argumentException => HandleArgumentException(argumentException),
            KeyNotFoundException notFoundException => HandleNotFoundException(notFoundException),
            UnauthorizedAccessException => HandleUnauthorizedException(),
            OperationCanceledException => HandleCancelledException(),
            _ => HandleUnexpectedException()
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "Request failed with {StatusCode}: {Message}", statusCode, exception.Message);

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), JsonOptions));
    }

    private static (int, ProblemDetails) HandleValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return ((int)HttpStatusCode.BadRequest, new ValidationProblemDetails(errors)
        {
            Title = "Validation Error",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "One or more validation errors occurred.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private static (int, ProblemDetails) HandleArgumentException(ArgumentException exception) =>
        ((int)HttpStatusCode.BadRequest, new ProblemDetails
        {
            Title = "Bad Request",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = exception.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });

    private static (int, ProblemDetails) HandleNotFoundException(KeyNotFoundException exception) =>
        ((int)HttpStatusCode.NotFound, new ProblemDetails
        {
            Title = "Not Found",
            Status = (int)HttpStatusCode.NotFound,
            Detail = exception.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
        });

    private static (int, ProblemDetails) HandleUnauthorizedException() =>
        ((int)HttpStatusCode.Unauthorized, new ProblemDetails
        {
            Title = "Unauthorized",
            Status = (int)HttpStatusCode.Unauthorized,
            Detail = "Invalid or missing API key",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });

    private static (int, ProblemDetails) HandleCancelledException() =>
        ((int)HttpStatusCode.BadRequest, new ProblemDetails
        {
            Title = "Request Cancelled",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "The request was cancelled by the client"
        });

    private static (int, ProblemDetails) HandleUnexpectedException() =>
        ((int)HttpStatusCode.InternalServerError, new ProblemDetails
        {
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = "An unexpected error occurred. Quote the traceId when contacting support.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        });
}
