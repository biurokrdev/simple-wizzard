import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay, tap } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { 
  RecipientDto, 
  BusinessLineDto, 
  TenantDto, 
  StatusDto,
  WorkerTypeDto,
  RefreshResult,
  RefreshAllResult 
} from '../data-access/external-data.model';

/**
 * Serwis do pobierania danych słownikowych z systemu zewnętrznego
 * Dane są cache'owane zarówno po stronie backendu (24h) jak i frontendu (shareReplay)
 * Umożliwia wymuszenie odświeżenia cache poprzez endpointy refresh
 */
@Injectable({
  providedIn: 'root'
})
export class ExternalDataService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/dictionaries`;

  constructor() {
    console.log('🌐 ExternalDataService initialized');
    console.log('📍 API Base URL:', this.baseUrl);
    console.log('🔧 Environment:', environment);
  }

  // Cache dla odbiorców - shareReplay zapewnia że request jest wykonany tylko raz
  private recipientsCache$?: Observable<RecipientDto[]>;
  private businessLinesCache$?: Observable<BusinessLineDto[]>;
  private tenantsCache$?: Observable<TenantDto[]>;
  private statusesCache$?: Observable<StatusDto[]>;
  private workerTypesCache$?: Observable<WorkerTypeDto[]>;

  /**
   * Pobiera listę wszystkich odbiorców
   */
  getRecipients(forceRefresh = false): Observable<RecipientDto[]> {
    if (!this.recipientsCache$ || forceRefresh) {
      this.recipientsCache$ = this.http
        .get<RecipientDto[]>(`${this.baseUrl}/recipients`)
        .pipe(shareReplay(1));
    }
    return this.recipientsCache$;
  }

  /**
   * Pobiera listę aktywnych odbiorców
   */
  getActiveRecipients(forceRefresh = false): Observable<RecipientDto[]> {
    const url = `${this.baseUrl}/recipients`;
    console.log('📡 Calling API:', url);
    
    if (forceRefresh) {
      return this.http.get<RecipientDto[]>(url);
    }
    return this.http
      .get<RecipientDto[]>(url)
      .pipe(shareReplay(1));
  }

  /**
   * Pobiera listę wszystkich linii biznesowych
   */
  getBusinessLines(forceRefresh = false): Observable<BusinessLineDto[]> {
    if (!this.businessLinesCache$ || forceRefresh) {
      this.businessLinesCache$ = this.http
        .get<BusinessLineDto[]>(`${this.baseUrl}/businesslines`)
        .pipe(shareReplay(1));
    }
    return this.businessLinesCache$;
  }

  /**
   * Pobiera listę aktywnych linii biznesowych
   */
  getActiveBusinessLines(forceRefresh = false): Observable<BusinessLineDto[]> {
    const url = `${this.baseUrl}/businesslines`;
    console.log('📡 Calling API:', url);
    
    if (forceRefresh) {
      return this.http.get<BusinessLineDto[]>(url);
    }
    return this.http
      .get<BusinessLineDto[]>(url)
      .pipe(shareReplay(1));
  }

  /**
   * Pobiera listę aktywnych tenantów
   */
  getActiveTenants(forceRefresh = false): Observable<TenantDto[]> {
    const url = `${this.baseUrl}/tenants`;
    console.log('📡 Calling API:', url);
    
    if (forceRefresh) {
      return this.http.get<TenantDto[]>(url);
    }
    return this.http
      .get<TenantDto[]>(url)
      .pipe(shareReplay(1));
  }

  /**
   * Pobiera listę wszystkich statusów
   */
  getStatuses(forceRefresh = false): Observable<StatusDto[]> {
    if (!this.statusesCache$ || forceRefresh) {
      this.statusesCache$ = this.http
        .get<StatusDto[]>(`${this.baseUrl}/statuses`)
        .pipe(shareReplay(1));
    }
    return this.statusesCache$;
  }

  /**
   * Pobiera listę aktywnych statusów
   */
  getActiveStatuses(forceRefresh = false): Observable<StatusDto[]> {
    const url = `${this.baseUrl}/statuses`;
    console.log('📡 Calling API:', url);
    
    if (forceRefresh) {
      return this.http.get<StatusDto[]>(url);
    }
    return this.http
      .get<StatusDto[]>(url)
      .pipe(shareReplay(1));
  }

  /**
   * Pobiera listę wszystkich typów workerów
   */
  getWorkerTypes(forceRefresh = false): Observable<WorkerTypeDto[]> {
    if (!this.workerTypesCache$ || forceRefresh) {
      this.workerTypesCache$ = this.http
        .get<WorkerTypeDto[]>(`${this.baseUrl}/workertypes`)
        .pipe(shareReplay(1));
    }
    return this.workerTypesCache$;
  }

  /**
   * Pobiera listę aktywnych typów workerów
   */
  getActiveWorkerTypes(forceRefresh = false): Observable<WorkerTypeDto[]> {
    const url = `${this.baseUrl}/workertypes`;
    console.log('📡 Calling API:', url);
    
    if (forceRefresh) {
      return this.http.get<WorkerTypeDto[]>(url);
    }
    return this.http
      .get<WorkerTypeDto[]>(url)
      .pipe(shareReplay(1));
  }

  /**
   * Odświeża cache odbiorców po stronie backendu i pobiera zaktualizowane dane
   */
  refreshRecipients(): Observable<RecipientDto[]> {
    console.log('🔄 Refreshing recipients cache');
    return this.http.put<RecipientDto[]>(`${this.baseUrl}/recipients/cache`, {}).pipe(
      tap(() => this.recipientsCache$ = undefined)
    );
  }

  /**
   * Odświeża cache linii biznesowych po stronie backendu i pobiera zaktualizowane dane
   */
  refreshBusinessLines(): Observable<BusinessLineDto[]> {
    console.log('🔄 Refreshing business lines cache');
    return this.http.put<BusinessLineDto[]>(`${this.baseUrl}/businesslines/cache`, {}).pipe(
      tap(() => this.businessLinesCache$ = undefined)
    );
  }

  /**
   * Odświeża cache tenantów po stronie backendu i pobiera zaktualizowane dane
   */
  refreshTenants(): Observable<TenantDto[]> {
    console.log('🔄 Refreshing tenants cache');
    return this.http.put<TenantDto[]>(`${this.baseUrl}/tenants/cache`, {}).pipe(
      tap(() => this.tenantsCache$ = undefined)
    );
  }

  /**
   * Odświeża cache statusów po stronie backendu i pobiera zaktualizowane dane
   */
  refreshStatuses(): Observable<StatusDto[]> {
    console.log('🔄 Refreshing statuses cache');
    return this.http.put<StatusDto[]>(`${this.baseUrl}/statuses/cache`, {}).pipe(
      tap(() => this.statusesCache$ = undefined)
    );
  }

  /**
   * Odświeża cache typów workerów po stronie backendu i pobiera zaktualizowane dane
   */
  refreshWorkerTypes(): Observable<WorkerTypeDto[]> {
    console.log('🔄 Refreshing worker types cache');
    return this.http.put<WorkerTypeDto[]>(`${this.baseUrl}/workertypes/cache`, {}).pipe(
      tap(() => this.workerTypesCache$ = undefined)
    );
  }

  /**
   * Odświeża wszystkie słowniki jednocześnie
   */
  refreshAllDictionaries(): Observable<RefreshAllResult> {
    console.log('🔄 Refreshing all dictionaries');
    const baseApiUrl = `${environment.apiUrl}/v1/dictionaries`;
    return this.http.put<RefreshAllResult>(`${baseApiUrl}/cache`, {}).pipe(
      tap(() => this.clearCache())
    );
  }

  /**
   * Czyści cache po stronie frontendu - przydatne gdy trzeba odświeżyć dane
   */
  clearCache(): void {
    this.recipientsCache$ = undefined;
    this.businessLinesCache$ = undefined;
    this.tenantsCache$ = undefined;
    this.statusesCache$ = undefined;
    this.workerTypesCache$ = undefined;
    console.log('🧹 Frontend cache cleared');
  }
}
