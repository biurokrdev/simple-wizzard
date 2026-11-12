using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using D2ServicesCreator.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace D2ServicesCreator.Api.Middleware;

/// <summary>
/// Rejects every request that does not carry the configured key in the <c>X-Api-Key</c> header.
/// Health checks and Swagger are exempt (the Swagger UI sends the key itself via "Authorize").
/// This is deliberately a single static key: the Services API authenticates an application, not a user,
/// and D2 Creator has no user identity to delegate to (R-03).
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware
{
    private static readonly PathString[] ExemptPaths = { new("/health"), new("/swagger") };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly IOptions<ApiKeyOptions> _options;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IOptions<ApiKeyOptions> options,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsExempt(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var expectedKey = _options.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            // Startup validation should have prevented this; treat it as "nobody is authorised" rather than "everybody is".
            _logger.LogError("Secrets:ApiKey is empty — rejecting {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteUnauthorizedAsync(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyOptions.HeaderName, out var providedKey)
            || !FixedTimeEquals(providedKey.ToString(), expectedKey))
        {
            _logger.LogWarning("Rejected {Method} {Path}: missing or invalid API key", context.Request.Method, context.Request.Path);
            await WriteUnauthorizedAsync(context);
            return;
        }

        await _next(context);
    }

    private static bool IsExempt(PathString path) =>
        ExemptPaths.Any(exempt => path.StartsWithSegments(exempt));

    // Constant-time comparison so response timing does not reveal how many leading bytes matched.
    private static bool FixedTimeEquals(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));

    private static async Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.WWWAuthenticate = "ApiKey";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = $"Missing or invalid API key (header {ApiKeyOptions.HeaderName}).",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
