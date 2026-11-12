using System.Text.Json;
using D2ServicesCreator.Api.Middleware;
using D2ServicesCreator.Api.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace D2ServicesCreator.Tests.Unit.Middleware;

public class ApiKeyAuthenticationMiddlewareTests
{
    private const string ConfiguredKey = "s3cret-key";

    [Fact]
    public async Task InvokeAsync_WhenHeaderMissing_Returns401ProblemAndSkipsPipeline()
    {
        var (context, nextCalled) = await Invoke(ConfiguredKey, path: "/api/v1/Projects/" + Guid.NewGuid(), header: null);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        context.Response.ContentType.Should().Be("application/problem+json");
        context.Response.Headers.WWWAuthenticate.ToString().Should().Be("ApiKey");
        ReadBody(context).RootElement.GetProperty("title").GetString().Should().Be("Unauthorized");
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyInvalid_Returns401()
    {
        var (context, nextCalled) = await Invoke(ConfiguredKey, "/api/v1/Projects", header: "wrong-key");

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyDiffersOnlyInCase_Returns401()
    {
        var (context, nextCalled) = await Invoke(ConfiguredKey, "/api/v1/Projects", header: ConfiguredKey.ToUpperInvariant());

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenKeyValid_CallsNext()
    {
        var (context, nextCalled) = await Invoke(ConfiguredKey, "/api/v1/Projects", header: ConfiguredKey);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/swagger")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task InvokeAsync_ForExemptPaths_CallsNextWithoutKey(string path)
    {
        var (_, nextCalled) = await Invoke(ConfiguredKey, path, header: null);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenPathMerelyStartsWithHealthText_StillRequiresKey()
    {
        // "/healthz-admin" is not the "/health" segment — segment matching, not prefix matching.
        var (context, nextCalled) = await Invoke(ConfiguredKey, "/healthz-admin", header: null);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task InvokeAsync_WhenConfiguredKeyEmpty_RejectsEveryRequest()
    {
        // Defence in depth: an empty key must never mean "no authentication".
        var (context, nextCalled) = await Invoke(configuredKey: "", "/api/v1/Projects", header: "");

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    private static async Task<(DefaultHttpContext Context, bool NextCalled)> Invoke(string configuredKey, string path, string? header)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (header is not null)
            context.Request.Headers[ApiKeyOptions.HeaderName] = header;

        var nextCalled = false;
        var middleware = new ApiKeyAuthenticationMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            Options.Create(new ApiKeyOptions { ApiKey = configuredKey }),
            NullLogger<ApiKeyAuthenticationMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private static JsonDocument ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return JsonDocument.Parse(reader.ReadToEnd());
    }
}
