import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

/** Profil workera — typ z katalogu aplikacji docelowej (albo ręczny) z opisem i domyślną konfiguracją (BR-016). */
export interface WorkerProfile {
  id: string;
  /** Typ workera = `workers.worker_type` (np. „Podstaw wartość”). */
  key: string;
  displayName: string;
  description: string | null;
  /** JSON jako tekst; token `{name}` = nazwa elementu. */
  defaultConfigJson: string;
  isVisible: boolean;
  sortOrder: number;
  source: 'Synced' | 'Manual';
  isMissingInSource: boolean;
  lastSyncedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface WorkerProfileSave {
  displayName: string;
  description: string | null;
  defaultConfigJson: string;
  isVisible: boolean;
  sortOrder: number;
}

export interface WorkerProfileCreate extends WorkerProfileSave {
  key: string;
}

export interface WorkerProfileSyncResult {
  source: string;
  syncedAt: string;
  added: string[];
  confirmed: string[];
  missing: string[];
  total: number;
}

@Injectable({ providedIn: 'root' })
export class WorkerProfileService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/workerprofiles`;

  /** Wszystkie profile (panel administracyjny). */
  getAll(): Observable<WorkerProfile[]> {
    return this.http.get<WorkerProfile[]>(this.baseUrl);
  }

  /** Profile udostępnione użytkownikom kreatora (dialog mapowania). */
  getVisible(): Observable<WorkerProfile[]> {
    return this.http.get<WorkerProfile[]>(`${this.baseUrl}/visible`);
  }

  create(data: WorkerProfileCreate): Observable<WorkerProfile> {
    return this.http.post<WorkerProfile>(this.baseUrl, data);
  }

  update(id: string, data: WorkerProfileSave): Observable<WorkerProfile> {
    return this.http.put<WorkerProfile>(`${this.baseUrl}/${id}`, data);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** Synchronizacja z katalogiem aplikacji docelowej — nowe typy trafiają jako ukryte. */
  sync(): Observable<WorkerProfileSyncResult> {
    return this.http.post<WorkerProfileSyncResult>(`${this.baseUrl}/sync`, {});
  }
}

/** Podstawia nazwę elementu w domyślnej konfiguracji profilu (token `{name}`). */
export function applyProfileDefaultConfig(profile: Pick<WorkerProfile, 'defaultConfigJson'>, elementName: string): string {
  const raw = profile.defaultConfigJson?.trim() || '{}';
  const filled = raw.split('{name}').join(elementName.trim());
  try {
    return JSON.stringify(JSON.parse(filled), null, 2);
  } catch {
    return filled;
  }
}
