using System.Reflection;
using System.Text.Json.Serialization;
using D2ApiCreator.Application;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Infrastructure;
using D2ApiCreator.Infrastructure.Common;
using D2ServicesCreator.Api.Extensions;
using D2ServicesCreator.Api.Security;
using D2ServicesCreator.Api.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Standard observability D2 (jak D2ApiCreator.Api): MS Logging, strukturalne logi JSON z polem `severity`
// → Cloud Logging / ELK pokazują ERROR/CRITICAL poprawnie. Lokalnie (Development) zostaje czytelny formatter.
// Sterowanie configiem `Logging:UseGcpFormat`.
builder.AddGcpStructuredLogging();

// Shared layers from D2ApiCreator: the same handlers, repositories and DbContext as the GUI API.
// The Services API exposes only a narrow subset of them (see Controllers/ProjectsController.cs).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// This host does not handle template binaries. Without GoogleCloudStorage:BucketName Infrastructure
// registers no IFileStorageService, and DI validation would reject the shared UploadFileCommandHandler.
builder.Services.TryAddScoped<IFileStorageService, UnavailableFileStorageService>();

// App-to-app authentication: static key in the X-Api-Key header. Fails fast when not configured.
builder.Services.AddApiKeyAuthentication(builder.Configuration);

builder.Services.AddControllers(options =>
    {
        // JSON only — no text/plain negotiation for string results (keeps Swagger response types honest).
        options.OutputFormatters.RemoveType<Microsoft.AspNetCore.Mvc.Formatters.StringOutputFormatter>();
    })
    .AddJsonOptions(options =>
    {
        // Enums as names on the wire (Draft/Active/Inactive) — readable for external systems
        // and rendered as an option list in Swagger. This is a separate contract from /api/v1 of D2ApiCreator.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "D2 Services Creator API",
        Version = "v1",
        Description = """
            REST API dla systemów zewnętrznych integrujących się z D2 Creator.
            Uwaga: to API jest przeznaczone dla aplikacji zewnętrznych, nie dla frontendu (GUI używa D2ApiCreator /api/v1).

            Zakres jest celowo minimalny: odczyt statusu projektu pisma oraz aktywacja / dezaktywacja jego wersji.
            Każde wywołanie wymaga nagłówka X-Api-Key.
            """
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Klucz API wymagany przez wszystkie endpointy. Nagłówek: X-Api-Key: <klucz>",
        In = ParameterLocation.Header,
        Name = ApiKeyOptions.HeaderName,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ApiKey", document)] = []
    });
});

// CORS only when origins are configured — this API is meant for server-to-server calls.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ExternalIntegrations", policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader());
    });
}

builder.Services.AddHealthChecks();

var app = builder.Build();

LogDatabaseConfiguration(app);

// Middleware pipeline — order matters. Observability is outermost so its correlation-id scope wraps
// every log (including exceptions) and the access log captures the final status code.
app.UseRequestObservability();

// ProblemDetails for every unhandled exception; never leaks stack traces.
app.UseExceptionHandlingMiddleware();

var swaggerEnabled = builder.Configuration.GetValue<bool?>("Swagger:Enabled")
    ?? (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker");
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "D2 Services Creator API v1");
        c.RoutePrefix = "swagger";
    });
}

if (app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

if (allowedOrigins.Length > 0)
{
    app.UseCors("ExternalIntegrations");
}

// After Swagger (the UI itself is public; its "Authorize" button sends the key) and before controllers.
app.UseApiKeyAuthentication();

app.MapControllers();
app.MapHealthChecks("/health");

app.Logger.LogInformation("D2ServicesCreator API started ({Environment})", app.Environment.EnvironmentName);

app.Run();

// Same safe connection-string diagnostics as D2ApiCreator.Api (redacted), through ILogger.
static void LogDatabaseConfiguration(WebApplication app)
{
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        app.Logger.LogWarning("No connection string configured. Please set ConnectionStrings:DefaultConnection");
        return;
    }

    var (isValid, errorMessage) = ConnectionStringHelper.ValidateConnectionString(connectionString);
    if (isValid)
    {
        app.Logger.LogInformation("Database connection configured: {ConnectionString}, Database: {DatabaseName}",
            ConnectionStringHelper.RedactConnectionString(connectionString),
            ConnectionStringHelper.GetDatabaseName(connectionString));
    }
    else
    {
        app.Logger.LogWarning("Invalid connection string: {ErrorMessage}", errorMessage);
    }
}

// Make Program accessible for integration tests
public partial class Program { }
