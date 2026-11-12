namespace D2ServicesCreator.Api.Security;

/// <summary>
/// Configuration of the app-to-app API key (section <c>Secrets</c>, key <c>ApiKey</c>;
/// in containers: <c>Secrets__ApiKey</c>). The key is a secret — never commit a real value.
/// </summary>
public sealed class ApiKeyOptions
{
    public const string SectionName = "Secrets";
    public const string HeaderName = "X-Api-Key";

    public string ApiKey { get; set; } = string.Empty;
}
