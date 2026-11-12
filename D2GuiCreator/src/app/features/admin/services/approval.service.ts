import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ProjectVersionResponse } from '../../letter-wizard/services/wizard-data.service';
import { LetterWizardStepKey } from '../../letter-wizard/data-access/letter-wizard.model';

/** Etykiety kroków kreatora — do uwag recenzenta (karta wersji, kreator). */
export const WIZARD_STEP_LABELS: Record<LetterWizardStepKey, string> = {
  documentDescription: 'Opis dokumentu',
  templateAndSchema: 'Szablon i schemat danych',
  configuration: 'Konfiguracja',
  summary: 'Podsumowanie'
};

export const WIZARD_STEP_ORDER: readonly LetterWizardStepKey[] = ['documentDescription', 'templateAndSchema', 'configuration', 'summary'];

/** Uwagi do kroków w kolejności kroków kreatora (pomija puste). */
export function orderedReviewNotes(notes: Record<string, string> | null | undefined): Array<{ step: LetterWizardStepKey; label: string; text: string }> {
  if (!notes) {
    return [];
  }
  return WIZARD_STEP_ORDER
    .filter((step) => !!notes[step]?.trim())
    .map((step) => ({ step, label: WIZARD_STEP_LABELS[step], text: notes[step].trim() }));
}

/** Wersja czekająca na decyzję administratora (BR-017). */
export interface PendingVersion {
  versionId: string;
  projectId: string;
  projectName: string;
  source: string;
  tenantId: string | null;
  versionTag: string;
  submittedAt: string | null;
  createdBy: string | null;
  templateName: string | null;
}

/** Przegląd i akceptacja wersji pisma: `/api/v1/approvals`. */
@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/v1/approvals`;

  getPending(): Observable<PendingVersion[]> {
    return this.http.get<PendingVersion[]>(`${this.baseUrl}/pending`);
  }

  /** Projektant: „Wyślij do weryfikacji” (Draft | RequiresChanges → PendingApproval). */
  submit(versionId: string, submittedBy: string | null): Observable<ProjectVersionResponse> {
    return this.http.post<ProjectVersionResponse>(`${this.baseUrl}/${versionId}/submit`, { submittedBy });
  }

  approve(versionId: string, reviewer: string, comment: string | null): Observable<ProjectVersionResponse> {
    return this.http.post<ProjectVersionResponse>(`${this.baseUrl}/${versionId}/approve`, { reviewer, comment });
  }

  reject(versionId: string, reviewer: string, comment: string | null): Observable<ProjectVersionResponse> {
    return this.http.post<ProjectVersionResponse>(`${this.baseUrl}/${versionId}/reject`, { reviewer, comment });
  }

  /** Uwagi recenzenta do kroków (zapis w trakcie przeglądu; tylko wersja „Do akceptacji”). */
  saveNotes(versionId: string, notes: Record<string, string>): Observable<ProjectVersionResponse> {
    return this.http.put<ProjectVersionResponse>(`${this.baseUrl}/${versionId}/notes`, { notes });
  }
}

/** Etykiety statusów wersji (enum `ProjectStatus` przychodzi jako liczba albo nazwa). */
export function versionStatusLabel(status: number | string | null | undefined): string {
  const labels: Record<string, string> = {
    '0': 'Robocza', Draft: 'Robocza',
    '1': 'Do akceptacji', PendingApproval: 'Do akceptacji',
    '2': 'Zaakceptowana', Approved: 'Zaakceptowana',
    '3': 'Wdrażanie', Deploying: 'Wdrażanie',
    '4': 'Aktywna', Active: 'Aktywna',
    '5': 'Nieaktywna', Inactive: 'Nieaktywna',
    '6': 'Wymaga poprawy', RequiresChanges: 'Wymaga poprawy'
  };
  if (status === null || status === undefined) {
    return '-';
  }
  return labels[String(status)] ?? String(status);
}

/** Klucz CSS statusu (do odznak). */
export function versionStatusKey(status: number | string | null | undefined): string {
  const keys: Record<string, string> = {
    '0': 'draft', Draft: 'draft', '1': 'pending', PendingApproval: 'pending', '2': 'approved', Approved: 'approved',
    '3': 'deploying', Deploying: 'deploying', '4': 'active', Active: 'active', '5': 'inactive', Inactive: 'inactive',
    '6': 'changes', RequiresChanges: 'changes'
  };
  return keys[String(status)] ?? 'draft';
}
