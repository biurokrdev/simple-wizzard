using D2ApiCreator.Api.Middleware;

namespace D2ApiCreator.Api.Extensions;

/// <summary>
/// Extension methods for configuring middleware
/// </summary>
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

