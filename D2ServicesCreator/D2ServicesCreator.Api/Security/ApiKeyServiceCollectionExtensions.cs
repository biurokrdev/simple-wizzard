namespace D2ServicesCreator.Api.Security;

public static class ApiKeyServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="ApiKeyOptions"/> and refuses to start when no key is configured. An empty key
    /// would otherwise either lock everyone out (401 on every call) or — worse — tempt a "temporary"
    /// bypass; failing fast makes the misconfiguration visible at deploy time instead.
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ApiKeyOptions.SectionName);
        services.Configure<ApiKeyOptions>(section);

        if (string.IsNullOrWhiteSpace(section[nameof(ApiKeyOptions.ApiKey)]))
        {
            throw new InvalidOperationException(
                $"{ApiKeyOptions.SectionName}:{nameof(ApiKeyOptions.ApiKey)} is not configured. " +
                "The Services API refuses to start without an API key — set Secrets__ApiKey (environment) " +
                "or appsettings.{Environment}.json for local development.");
        }

        return services;
    }
}
