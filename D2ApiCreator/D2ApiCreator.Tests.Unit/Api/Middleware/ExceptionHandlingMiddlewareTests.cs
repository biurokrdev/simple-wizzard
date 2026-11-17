using System.Net;
using System.Text.Json;
using D2ApiCreator.Api.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace D2ApiCreator.Tests.Unit.Api.Middleware;

/// <summary>
/// Globalna obsługa wyjątków: mapowanie typów wyjątków na ProblemDetails (RFC 7807)
/// z właściwym kodem HTTP i content-type. Bez przeciekania szczegółów ani stack trace (R-04).
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, string ContentType, JsonDocument Body)> InvokeWith(
        Exception toThrow, string? correlationId = null)
    {
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        if (correlationId is not null)
            context.Items[RequestObservabilityMiddleware.CorrelationIdItemKey] = correlationId;

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw toThrow, NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var text = await reader.ReadToEndAsync();
        return (context.Response.StatusCode, context.Response.ContentType ?? string.Empty, JsonDocument.Parse(text));
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = new ExceptionHandlingMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400ProblemDetails()
    {
        var ex = new ValidationException(new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Email", "Email invalid")
        });

        var (statusCode, contentType, body) = await InvokeWith(ex);

        statusCode.Should().Be((int)HttpStatusCode.BadRequest);
        contentType.Should().Be("application/problem+json");
        body.RootElement.GetProperty("status").GetInt32().Should().Be((int)HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("title").GetString().Should().Be("Błąd walidacji");
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_EmitsGroupedErrorsInBody()
    {
        var ex = new ValidationException(new[]
        {
            new ValidationFailure("Name", "Name is required"),
            new ValidationFailure("Name", "Name too short"),
            new ValidationFailure("Email", "Email invalid")
        });

        var (_, _, body) = await InvokeWith(ex);

        // ValidationProblemDetails.Errors must be serialized via the runtime type.
        var errors = body.RootElement.GetProperty("errors");
        errors.GetProperty("Name").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(new[] { "Name is required", "Name too short" });
        errors.GetProperty("Email").EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(new[] { "Email invalid" });
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_DoesNotLeakStackTrace()
    {
        var ex = new ValidationException(new[] { new ValidationFailure("Name", "Name is required") });

        var (_, _, body) = await InvokeWith(ex);

        var json = body.RootElement.GetRawText();
        json.Should().NotContain("StackTrace").And.NotContain("   at ").And.NotContain("details");
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400WithMessage()
    {
        var (statusCode, _, body) = await InvokeWith(new ArgumentException("zła wartość"));

        statusCode.Should().Be((int)HttpStatusCode.BadRequest);
        body.RootElement.GetProperty("detail").GetString().Should().Be("zła wartość");
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFound_Returns404()
    {
        var (statusCode, _, body) = await InvokeWith(new KeyNotFoundException("nie ma takiego"));

        statusCode.Should().Be((int)HttpStatusCode.NotFound);
        body.RootElement.GetProperty("detail").GetString().Should().Be("nie ma takiego");
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccess_Returns401()
    {
        var (statusCode, _, body) = await InvokeWith(new UnauthorizedAccessException("secret internals"));

        statusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        body.RootElement.GetProperty("title").GetString().Should().Be("Brak autoryzacji");
        body.RootElement.GetRawText().Should().NotContain("secret internals");
    }

    [Fact]
    public async Task InvokeAsync_OperationCanceled_Returns400()
    {
        var (statusCode, _, _) = await InvokeWith(new OperationCanceledException());

        statusCode.Should().Be((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_Returns500WithoutLeakingDetails()
    {
        var (statusCode, _, body) = await InvokeWith(new InvalidOperationException("internal secret detail"));

        statusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        body.RootElement.GetProperty("detail").GetString()
            .Should().NotContain("internal secret detail")
            .And.Contain("nieoczekiwany błąd");
        body.RootElement.GetRawText().Should().NotContain("   at ");
    }

    [Fact]
    public async Task InvokeAsync_SerializesProblemDetailsAsCamelCase()
    {
        var (_, _, body) = await InvokeWith(new ArgumentException("x"));

        body.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        body.RootElement.TryGetProperty("status", out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenCorrelationIdInContext_IncludesItInProblemDetailsExtensions()
    {
        var (_, _, body) = await InvokeWith(new ArgumentException("x"), correlationId: "corr-42");

        body.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-42");
    }
}
