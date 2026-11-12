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
using Serilog;

// Bootstrap logger — replaced by the configured one (appsettings "Serilog" section) once the host is built.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/d2services-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting D2ServicesCreator API (external integrations)");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Same safe connection-string diagnostics as D2ApiCreator.Api.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        var (isValid, errorMessage) = ConnectionStringHelper.ValidateConnectionString(connectionString);
        if (isValid)
        {
            Log.Information("Database connection configured: {ConnectionString}, Database: {DatabaseName}",
                ConnectionStringHelper.RedactConnectionString(connectionString),
                ConnectionStringHelper.GetDatabaseName(connectionString));
        }
        else
        {
            Log.Warning("Invalid connection string: {ErrorMessage}", errorMessage);
        }
    }
    else
    {
        Log.Warning("No connection string configured. Please set ConnectionStrings:DefaultConnection");
    }

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

    app.UseSerilogRequestLogging();

    // ProblemDetails for every unhandled exception; never leaks stack traces (unlike D2ApiCreator.Api, see R-04).
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

    Log.Information("D2ServicesCreator API started");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "D2ServicesCreator API terminated unexpectedly");
    // Rethrow on purpose: a swallowed startup failure would look like a clean exit to the container
    // runtime and would also break WebApplicationFactory in integration tests (R-09 in D2ApiCreator).
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for integration tests
public partial class Program { }
