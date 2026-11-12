using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using D2ApiCreator.Application.Common.Interfaces;
using D2ApiCreator.Domain.Repositories;
using D2ApiCreator.Infrastructure.Services.WorkerCatalog;
using D2ApiCreator.Infrastructure.Services.Packages;
using D2ApiCreator.Infrastructure.Persistence;
using D2ApiCreator.Infrastructure.Persistence.Repositories;
using D2ApiCreator.Infrastructure.Services;

namespace D2ApiCreator.Infrastructure;

/// <summary>
/// Dependency Injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Database Context (without migrations - using SQL scripts instead)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register Unit of Work
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Register generic repository
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        // Register specific repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectVersionRepository, ProjectVersionRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IWorkerRepository, WorkerRepository>();
        services.AddScoped<IWorkerProfileRepository, WorkerProfileRepository>();

        // Katalog typów workerów aplikacji docelowej (BR-016): HTTP gdy skonfigurowany adres, inaczej wbudowany (R-31)
        services.Configure<WorkerCatalogOptions>(configuration.GetSection(WorkerCatalogOptions.SectionName));
        var catalogUrl = configuration[$"{WorkerCatalogOptions.SectionName}:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(catalogUrl))
        {
            services.AddHttpClient<IWorkerCatalogSource, HttpWorkerCatalogSource>((provider, client) =>
            {
                var seconds = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<WorkerCatalogOptions>>().Value.TimeoutSeconds;
                client.Timeout = TimeSpan.FromSeconds(seconds <= 0 ? 15 : seconds);
            });
        }
        else
        {
            services.AddSingleton<IWorkerCatalogSource, StaticWorkerCatalogSource>();
        }

        // Paczki wdrożeniowe (BR-022): wysyłka HTTP, gdy skonfigurowany adres aplikacji docelowej; inaczej tylko pobieranie
        services.Configure<TargetApplicationOptions>(configuration.GetSection(TargetApplicationOptions.SectionName));
        var packageUrl = configuration[$"{TargetApplicationOptions.SectionName}:PackageUrl"];
        if (!string.IsNullOrWhiteSpace(packageUrl))
        {
            services.AddHttpClient<IPackageDeliveryService, HttpPackageDeliveryService>((provider, client) =>
            {
                var seconds = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TargetApplicationOptions>>().Value.TimeoutSeconds;
                client.Timeout = TimeSpan.FromSeconds(seconds <= 0 ? 60 : seconds);
            });
        }
        else
        {
            services.AddSingleton<IPackageDeliveryService, NotConfiguredPackageDeliveryService>();
        }

        // Register services
        services.AddScoped<IDocumentPlaceholderService, DocumentPlaceholderService>();
        services.AddScoped<ISpreadsheetTemplateService, SpreadsheetTemplateService>();
        services.AddScoped<IDocumentValidationService, MockDocumentValidationService>();

        // Register Cache Service
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Register External Data Service (mock - to be replaced with real implementation)
        services.AddScoped<IExternalDataService, MockExternalDataService>();

        // Google Cloud Storage — binaria szablonów (files.storage_ref), jak w D2 ViewerEditor (importer)
        services.AddGoogleCloudStorage(configuration);

        return services;
    }

    /// <summary>
    /// Rejestruje <see cref="StorageClient"/> i <see cref="IFileStorageService"/> na podstawie sekcji
    /// <c>GoogleCloudStorage</c>. Pusty <c>ApiEndpoint</c> = prawdziwy GCS (plik service account albo ADC / Workload Identity);
    /// ustawiony <c>ApiEndpoint</c> = fake-gcs-server bez autoryzacji (DEV/test).
    /// Bez <c>BucketName</c> storage nie jest rejestrowany — upload plików zakończy się błędem DI, a nie cichym zapisem do bazy.
    /// </summary>
    private static IServiceCollection AddGoogleCloudStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GcsStorageOptions.SectionName);
        var gcsOptions = new GcsStorageOptions
        {
            BucketName = section[nameof(GcsStorageOptions.BucketName)] ?? string.Empty,
            ApiEndpoint = section[nameof(GcsStorageOptions.ApiEndpoint)],
            CredentialPath = section[nameof(GcsStorageOptions.CredentialPath)]
        };

        services.Configure<GcsStorageOptions>(o =>
        {
            o.BucketName = gcsOptions.BucketName;
            o.ApiEndpoint = gcsOptions.ApiEndpoint;
            o.CredentialPath = gcsOptions.CredentialPath;
        });

        if (string.IsNullOrWhiteSpace(gcsOptions.BucketName))
        {
            return services;
        }

        services.AddSingleton(_ =>
        {
            if (!string.IsNullOrWhiteSpace(gcsOptions.ApiEndpoint))
            {
                // DEV/test: fake-gcs-server — bez autoryzacji, własny endpoint
                var builder = new StorageClientBuilder
                {
                    BaseUri = gcsOptions.ApiEndpoint.TrimEnd('/') + "/storage/v1/",
                    UnauthenticatedAccess = true
                };
                return builder.Build();
            }

            if (!string.IsNullOrWhiteSpace(gcsOptions.CredentialPath))
            {
                // Produkcja z plikiem service account
                var credential = GoogleCredential.FromFile(gcsOptions.CredentialPath);
                return StorageClient.Create(credential);
            }

            // Produkcja: Workload Identity / Application Default Credentials
            return StorageClient.Create();
        });

        services.AddScoped<IFileStorageService, GcsFileStorageService>();

        return services;
    }
}
