using D2ApiCreator.Api.Extensions;
using D2ApiCreator.Application;
using D2ApiCreator.Infrastructure;
using D2ApiCreator.Infrastructure.Common;

var builder = WebApplication.CreateBuilder(args);

// Standard observability D2 (jak D2ViewerEditor.Api): MS Logging, strukturalne logi JSON z polem `severity`
// → Cloud Logging / ELK pokazują ERROR/CRITICAL poprawnie. Lokalnie (Development) zostaje czytelny formatter.
builder.AddGcpStructuredLogging();

// Add services to the container
builder.Services.AddControllers();

// Add API Versioning with MVC
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Add Application Layer
builder.Services.AddApplication();

// Add Infrastructure Layer
builder.Services.AddInfrastructure(builder.Configuration);

// Add API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "D2ApiCreator API",
        Version = "v1",
        Description = "RESTful API built with DDD, Clean Architecture, and best practices"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "http://localhost:4201",
                "http://localhost:3000"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    // Backup policy for development
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

LogDatabaseConfiguration(app);

// Middleware pipeline — order matters. Observability is outermost so its correlation-id scope wraps
// every log (including exceptions) and the access log captures the final status code.
app.UseRequestObservability();
app.UseExceptionHandlingMiddleware();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "D2ApiCreator API V1");
    });
}

// HTTPS redirect only in Production (not in Development or Docker)
if (app.Environment.IsProduction() &&
    !app.Environment.EnvironmentName.Equals("Docker", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

// Use CORS - must be before UseAuthorization
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll"); // Allow all origins in development
}
else
{
    app.UseCors("AllowFrontend"); // Specific origins in production
}

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health").RequireCors("AllowAll");

app.Logger.LogInformation("D2ApiCreator Web API started ({Environment})", app.Environment.EnvironmentName);

app.Run();

// Validate and log the connection string (redacted) — same diagnostics as before, now through ILogger.
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
