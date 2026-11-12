using D2ServicesCreator.Api.Middleware;

namespace D2ServicesCreator.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();

    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app) =>
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
}
