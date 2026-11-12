using D2ApiCreator.Application.DTOs;

namespace D2ApiCreator.Infrastructure.Services;

/// <summary>
/// Interfejs dla serwisu dostarczającego dane zewnętrzne (odbiorcy, linie biznesowe, tenanci, statusy)
/// </summary>
public interface IExternalDataService
{
    /// <summary>
    /// Pobiera listę odbiorców
    /// </summary>
    Task<IReadOnlyList<RecipientDto>> GetRecipientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera listę linii biznesowych
    /// </summary>
    Task<IReadOnlyList<BusinessLineDto>> GetBusinessLinesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera listę tenantów
    /// </summary>
    Task<IReadOnlyList<TenantDto>> GetTenantsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pobiera listę statusów
    /// </summary>
    Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Mock implementacja serwisu danych zewnętrznych
/// Docelowo będzie zastąpiony przez prawdziwy serwis komunikujący się z zewnętrznym WebAPI
/// </summary>
public sealed class MockExternalDataService : IExternalDataService
{
    private readonly ICacheService _cacheService;
    private const string RecipientsCacheKey = "external_recipients";
    private const string BusinessLinesCacheKey = "external_business_lines";
    private const string TenantsCacheKey = "external_tenants";
    private const string StatusesCacheKey = "external_statuses";

    public MockExternalDataService(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    public async Task<IReadOnlyList<RecipientDto>> GetRecipientsAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            RecipientsCacheKey,
            () => Task.FromResult(GetMockRecipients()),
            TimeSpan.FromHours(24) // Cache na 24 godziny
        );
    }

    public async Task<IReadOnlyList<BusinessLineDto>> GetBusinessLinesAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            BusinessLinesCacheKey,
            () => Task.FromResult(GetMockBusinessLines()),
            TimeSpan.FromHours(24) // Cache na 24 godziny
        );
    }

    public async Task<IReadOnlyList<TenantDto>> GetTenantsAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            TenantsCacheKey,
            () => Task.FromResult(GetMockTenants()),
            TimeSpan.FromHours(24) // Cache na 24 godziny
        );
    }

    public async Task<IReadOnlyList<StatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            StatusesCacheKey,
            () => Task.FromResult(GetMockStatuses()),
            TimeSpan.FromHours(24) // Cache na 24 godziny
        );
    }

    private static IReadOnlyList<RecipientDto> GetMockRecipients()
    {
        return new List<RecipientDto>
        {
            new RecipientDto
            {
                Id = "1",
                Code = "REC001",
                Name = "Brak informacji",
                Description = "Brak informacji o odbiorcy",
                IsActive = true
            },
            new RecipientDto
            {
                Id = "2",
                Code = "REC002",
                Name = "Klient wewnętrzny",
                Description = "Odbiorcy wewnętrzni organizacji",
                IsActive = true
            },
            new RecipientDto
            {
                Id = "3",
                Code = "REC003",
                Name = "Klient zewnętrzny",
                Description = "Klienci zewnętrzni",
                IsActive = true
            },
            new RecipientDto
            {
                Id = "4",
                Code = "REC004",
                Name = "Klient zewnętrzny - WCAG",
                Description = "Klienci zewnętrzni z wymogami dostępności WCAG",
                IsActive = true
            },
            new RecipientDto
            {
                Id = "5",
                Code = "REC005",
                Name = "Klient wewnętrzny i zewnętrzny",
                Description = "Odbiorcy zarówno wewnętrzni jak i zewnętrzni",
                IsActive = true
            },
            new RecipientDto
            {
                Id = "6",
                Code = "REC006",
                Name = "Demo / testowe",
                Description = "Dokumenty demonstracyjne i testowe",
                IsActive = true
            }
        };
    }

    private static IReadOnlyList<BusinessLineDto> GetMockBusinessLines()
    {
        return new List<BusinessLineDto>
        {
            new BusinessLineDto
            {
                Id = "1",
                Code = "BB",
                Name = "BB",
                Description = "Bankowość Biznesowa",
                IsActive = true
            },
            new BusinessLineDto
            {
                Id = "2",
                Code = "WB",
                Name = "WB",
                Description = "Wealth Banking",
                IsActive = true
            },
            new BusinessLineDto
            {
                Id = "3",
                Code = "DEMO",
                Name = "DEMO",
                Description = "Środowisko demonstracyjne",
                IsActive = true
            },
            new BusinessLineDto
            {
                Id = "4",
                Code = "DETAL",
                Name = "DETAL",
                Description = "Bankowość Detaliczna",
                IsActive = true
            },
            new BusinessLineDto
            {
                Id = "5",
                Code = "KYC",
                Name = "KYC",
                Description = "Know Your Customer",
                IsActive = true
            }
        };
    }

    private static IReadOnlyList<TenantDto> GetMockTenants()
    {
        return new List<TenantDto>
        {
            new TenantDto
            {
                Id = "1",
                Code = "PL",
                Name = "Polska",
                Description = "Tenant dla Polski",
                IsActive = true
            },
            new TenantDto
            {
                Id = "2",
                Code = "NL",
                Name = "Holandia",
                Description = "Tenant dla Holandii",
                IsActive = true
            },
            new TenantDto
            {
                Id = "3",
                Code = "DE",
                Name = "Niemcy",
                Description = "Tenant dla Niemiec",
                IsActive = true
            },
            new TenantDto
            {
                Id = "4",
                Code = "FR",
                Name = "Francja",
                Description = "Tenant dla Francji",
                IsActive = false
            }
        };
    }

    private static IReadOnlyList<StatusDto> GetMockStatuses()
    {
        return new List<StatusDto>
        {
            new StatusDto
            {
                Id = "1",
                Code = "draft",
                Name = "Wersja robocza",
                Description = "Dokument w trakcie tworzenia",
                IsActive = true
            },
            new StatusDto
            {
                Id = "2",
                Code = "active",
                Name = "Aktywny",
                Description = "Dokument aktywny i gotowy do użycia",
                IsActive = true
            },
            new StatusDto
            {
                Id = "3",
                Code = "inactive",
                Name = "Nieaktywny",
                Description = "Dokument nieaktywny, niedostępny do użycia",
                IsActive = true
            },
            new StatusDto
            {
                Id = "4",
                Code = "archived",
                Name = "Zarchiwizowany",
                Description = "Dokument przeniesiony do archiwum",
                IsActive = true
            }
        };
    }
}
