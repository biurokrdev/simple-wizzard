using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace D2ApiCreator.Api.Controllers;

/// <summary>
/// Health check — dostępność API + informacje o buildzie (ten sam kształt co w D2 ViewerEditor:
/// GUI czyta <c>environment</c>, <c>buildNumber</c>, <c>buildDate</c> do stopki i banera środowiska).
/// </summary>
public class HealthController : BaseApiController
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Sprawdza dostępność API i zwraca informacje o buildzie.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var assembly = typeof(HealthController).Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

        return Ok(new HealthResponse(
            Status: "Healthy",
            Timestamp: DateTime.UtcNow,
            Version: version,
            Environment: _configuration["BuildInfo:Environment"]
                         ?? System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                         ?? "UNKNOWN",
            BuildNumber: version,
            BuildDate: ResolveBuildDate(assembly)));
    }

    /// <summary>Data ostatniego zapisu DLL API (UTC, <c>dd/MM/yyyy HH:mm</c> jak w D2 ViewerEditor); pusta, gdy niedostępna.</summary>
    private static string ResolveBuildDate(Assembly assembly)
    {
        try
        {
            var location = assembly.Location;
            if (string.IsNullOrEmpty(location) || !System.IO.File.Exists(location))
            {
                return string.Empty;
            }

            return System.IO.File.GetLastWriteTimeUtc(location)
                .ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// Odpowiedź health check. <c>Version</c> zostaje dla zgodności wstecznej (to samo co <c>BuildNumber</c>).
/// </summary>
public sealed record HealthResponse(
    string Status,
    DateTime Timestamp,
    string Version,
    string Environment,
    string BuildNumber,
    string BuildDate);
