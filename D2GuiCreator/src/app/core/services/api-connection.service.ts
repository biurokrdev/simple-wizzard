import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, interval, map, of, switchMap, timer } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Odpowiedź `GET /api/v1/health` (ten sam kształt co health D2 ViewerEditor). */
export interface ApiHealthResponse {
  status: string;
  timestamp: string;
  version?: string;
  environment?: string;
  buildNumber?: string;
  buildDate?: string;
}

/**
 * Monitor połączenia z API (health-check co 10 s) + dane o buildzie API.
 * Źródło dla shella (`shared/ui/shell`): `isApiConnected` (baner offline) oraz `buildNumber`/`buildDate` (stopka).
 */
@Injectable({
  providedIn: 'root'
})
export class ApiConnectionService {
  private readonly isConnected = signal<boolean>(true);
  private readonly _apiEnvironment = signal<string | null>(null);
  private readonly _buildNumber = signal<string | null>(null);
  private readonly _buildDate = signal<string | null>(null);
  private readonly checkInterval = 10000; // 10 sekund
  private readonly retryInterval = 5000; // 5 sekund gdy rozłączone

  readonly isApiConnected = this.isConnected.asReadonly();
  /** Nazwa środowiska zgłaszana przez API (`Development`, `Docker`, …) — informacyjnie; baner używa `environment.name` frontu. */
  readonly apiEnvironment = this._apiEnvironment.asReadonly();
  /** Numer buildu API z health-checka; `null` dopóki API nie odpowiedziało. */
  readonly buildNumber = this._buildNumber.asReadonly();
  /** Data buildu API z health-checka (`dd/MM/yyyy HH:mm`); `null` dopóki API nie odpowiedziało. */
  readonly buildDate = this._buildDate.asReadonly();

  constructor(private readonly http: HttpClient) {
    this.startHealthCheck();
  }

  /**
   * Wywołaj ręcznie po wykryciu błędu połączenia
   */
  markAsDisconnected(): void {
    this.isConnected.set(false);
    // Natychmiastowe sprawdzenie po 2 sekundach
    timer(2000).pipe(
      switchMap(() => this.checkApiHealth())
    ).subscribe(isHealthy => {
      this.isConnected.set(isHealthy);
    });
  }

  /**
   * Wywołaj po udanym połączeniu
   */
  markAsConnected(): void {
    this.isConnected.set(true);
  }

  private startHealthCheck(): void {
    // Pierwsze sprawdzenie od razu
    this.checkApiHealth().subscribe(isHealthy => {
      this.isConnected.set(isHealthy);
    });

    // Sprawdzaj regularnie co 10 sekund
    interval(this.checkInterval).pipe(
      switchMap(() => this.checkApiHealth())
    ).subscribe(isHealthy => {
      this.isConnected.set(isHealthy);
    });
  }

  private checkApiHealth() {
    // Używamy kontrolera health który zwraca JSON
    const healthUrl = `${environment.apiUrl}/v1/health`;

    return this.http.get<ApiHealthResponse>(healthUrl).pipe(
      map((response) => {
        this._apiEnvironment.set(nonEmpty(response.environment));
        this._buildNumber.set(nonEmpty(response.buildNumber) ?? nonEmpty(response.version));
        this._buildDate.set(nonEmpty(response.buildDate));
        return true;
      }),
      catchError((error) => {
        console.warn('[ApiConnection] Health check FAILED:', error?.status ?? error);
        return of(false);
      })
    );
  }
}

function nonEmpty(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed ? trimmed : null;
}
