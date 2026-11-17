using System.Net;
using System.Text.Json;
using D2ServicesCreator.Api.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace D2ServicesCreator.Tests.Unit.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var context = new DefaultHttpContext();
        var called = false;
        var middleware = new ExceptionHandlingMiddleware(
            _ => { called = true; return Task.CompletedTask; },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400ValidationProblemDetails()
    {
        var exception = new ValidationException(new[] { new ValidationFailure("VersionId", "Required") });

        var (statusCode, contentType, body) = await InvokeWith(exception);

        statusCode.Should().Be((int)HttpStatusCode.BadRequest);
        contentType.Should().Be("application/problem+json");
        body.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
        body.RootElement.GetProperty("errors").GetProperty("VersionId").EnumerateArray()
            .Select(e => e.GetString()).Should().Contain("Required");
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404()
    {
        var (statusCode, _, body) = await InvokeWith(new KeyNotFoundException("no such version"));

        statusCode.Should().Be((int)HttpStatusCode.NotFound);
        body.RootElement.GetProperty("detail").GetString().Should().Be("no such version");
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns401()
    {
        var (statusCode, _, body) = await InvokeWith(new UnauthorizedAccessException());

        statusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        body.RootElement.GetProperty("title").GetString().Should().Be("Unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_Returns500AndHidesInternalDetails()
    {
        var (statusCode, _, body) = await InvokeWith(new InvalidOperationException("internal details: connection string"));

        statusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var json = body.RootElement.GetRawText();
        json.Should().NotContain("internal details").And.NotContain("StackTrace").And.NotContain("   at ");
        body.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdInContext_IncludesItInProblemDetailsExtensions()
    {
        var (_, _, body) = await InvokeWith(new ArgumentException("x"), correlationId: "corr-42");

        body.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-42");
        body.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private static async Task<(int StatusCode, string ContentType, JsonDocument Body)> InvokeWith(
        Exception toThrow, string? correlationId = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        if (correlationId is not null)
            context.Items[RequestObservabilityMiddleware.CorrelationIdItemKey] = correlationId;
        var middleware = new ExceptionHandlingMiddleware(_ => throw toThrow, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var text = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, context.Response.ContentType ?? string.Empty, JsonDocument.Parse(text));
    }
}
