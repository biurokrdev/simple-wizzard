using D2ApiCreator.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace D2ApiCreator.Tests.Unit.Api.Middleware;

/// <summary>
/// Korelacja requestów (standard D2 ViewerEditor): nagłówek X-Correlation-ID jest echo'owany albo generowany,
/// a wartość ląduje w HttpContext.Items dla ExceptionHandlingMiddleware i formattera logów.
/// </summary>
public class RequestObservabilityMiddlewareTests
{
    private static RequestObservabilityMiddleware Build(RequestDelegate next) =>
        new(next, NullLogger<RequestObservabilityMiddleware>.Instance);

    [Fact]
    public async Task InvokeAsync_WhenHeaderMissing_GeneratesCorrelationIdAndSetsResponseHeader()
    {
        var context = new DefaultHttpContext();
        var middleware = Build(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[RequestObservabilityMiddleware.CorrelationIdHeader].ToString()
            .Should().NotBeNullOrWhiteSpace();
        context.Items[RequestObservabilityMiddleware.CorrelationIdItemKey].Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_WhenIncomingHeaderPresent_EchoesCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestObservabilityMiddleware.CorrelationIdHeader] = "corr-abc";
        var middleware = Build(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Response.Headers[RequestObservabilityMiddleware.CorrelationIdHeader].ToString()
            .Should().Be("corr-abc");
        context.Items[RequestObservabilityMiddleware.CorrelationIdItemKey].Should().Be("corr-abc");
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderIsWhitespace_GeneratesNewCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestObservabilityMiddleware.CorrelationIdHeader] = "   ";
        var middleware = Build(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var resolved = context.Response.Headers[RequestObservabilityMiddleware.CorrelationIdHeader].ToString();
        resolved.Should().NotBeNullOrWhiteSpace();
        resolved.Should().NotBe("   ");
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderTooLong_GeneratesNewCorrelationId()
    {
        var tooLong = new string('a', 129);
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestObservabilityMiddleware.CorrelationIdHeader] = tooLong;
        var middleware = Build(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var resolved = context.Response.Headers[RequestObservabilityMiddleware.CorrelationIdHeader].ToString();
        resolved.Should().NotBeNullOrWhiteSpace();
        resolved.Should().NotBe(tooLong);
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_StillPropagatesExceptionAfterAccessLog()
    {
        var context = new DefaultHttpContext();
        var middleware = Build(_ => throw new InvalidOperationException("boom"));

        var act = () => middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<InvalidOperationException>();
        context.Response.Headers[RequestObservabilityMiddleware.CorrelationIdHeader].ToString()
            .Should().NotBeNullOrWhiteSpace("the correlation id is set before the pipeline runs");
    }
}
