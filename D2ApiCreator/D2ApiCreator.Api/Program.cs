using D2ApiCreator.Application;
using D2ApiCreator.Infrastructure;
using D2ApiCreator.Infrastructure.Common;
using D2ApiCreator.Api.Extensions;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting D2ApiCreator Web API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Validate and log connection string (safely)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        var (isValid, errorMessage) = ConnectionStringHelper.ValidateConnectionString(connectionString);
        if (isValid)
        {
            var dbName = ConnectionStringHelper.GetDatabaseName(connectionString);
            var redactedConnStr = ConnectionStringHelper.RedactConnectionString(connectionString);
            Log.Information("Database connection configured: {ConnectionString}, Database: {DatabaseName}", 
                redactedConnStr, dbName);
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

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "D2ApiCreator API V1");
        });
    }

    // Global Exception Handler
    app.UseExceptionHandlingMiddleware();

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

    Log.Information("Application started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for integration tests
public partial class Program { }

