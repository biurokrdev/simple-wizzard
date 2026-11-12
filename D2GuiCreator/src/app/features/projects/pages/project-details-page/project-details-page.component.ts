import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { D2GridCellDef, D2GridComponent, D2GridDetailDef, GridColumn, GridLabels } from 'd2-gui-shared';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { WizardDataService, ProjectResponse, ProjectVersionResponse } from '../../../letter-wizard/services/wizard-data.service';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { WorkerService } from '../../../letter-wizard/services/worker.service';
import type { Worker } from '../../../letter-wizard/models/worker.model';
import { WorkerMappingPreviewComponent } from '../../../letter-wizard/components/worker-mapping-preview/worker-mapping-preview.component';
import { prettyPrintJson, readConfigKey } from '../../../letter-wizard/models/worker-config.util';
import { orderedReviewNotes, versionStatusKey, versionStatusLabel } from '../../../admin/services/approval.service';
import { EmptyStateComponent } from '../../../../shared/ui/empty-state/empty-state.component';
import { CONTENT_CONTROL_KIND_HINTS, DetectedElementRow, buildDetectedElementRows, elementBadgeClass } from '../../../letter-wizard/models/content-control-kind';
import { AuthService } from '../../../../core/services/auth.service';

/** Skąd otwarto szczegóły — decyduje, dokąd prowadzi „Wróć” (lista projektanta albo moduł administracyjny). */
type DetailsOrigin = 'projects' | 'admin-projects' | 'admin-approvals';

@Component({
  selector: 'app-project-details-page',
  standalone: true,
  imports: [CommonModule, PageLayoutComponent, ButtonComponent, D2GridComponent, D2GridCellDef, D2GridDetailDef, WorkerMappingPreviewComponent, EmptyStateComponent],
  templateUrl: './project-details-page.component.html',
  styleUrls: ['./project-details-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDetailsPageComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly wizardDataService = inject(WizardDataService);
  private readonly workerService = inject(WorkerService);
  private readonly auth = inject(AuthService);

  readonly projectId = signal<string | null>(null);

  /** Paczka wdrożeniowa (BR-022): identyfikator wersji w trakcie budowania / wysyłki i ostatni komunikat. */
  readonly packageBusyId = signal<string | null>(null);
  readonly packageNotice = signal<{ kind: 'success' | 'error'; text: string } | null>(null);
  readonly project = signal<ProjectResponse | null>(null);
  readonly versions = signal<ProjectVersionResponse[]>([]);
  readonly latestVersion = signal<ProjectVersionResponse | null>(null);
  /** Wersja aktywna projektu (flaga `isActive` z API) — decyduje o statusie w sekcji „Ogólne". */
  readonly activeVersion = computed(() => this.versions().find(v => v.isActive) ?? null);
  /** Górny przycisk: edycja aktywnej wersji = nowa wersja robocza (kopia), inaczej edycja w miejscu. */
  readonly editButtonLabel = computed(() => this.latestVersion()?.isActive ? 'Nowa wersja' : 'Edytuj');
  readonly workers = signal<Worker[]>([]);
  readonly isLoading = signal(false);
  readonly isLoadingVersions = signal(false);
  readonly isLoadingWorkers = signal(false);
  /** Błąd wczytywania projektu (np. usunięty / brak dostępu) — stan błędu zamiast pustej strony. */
  readonly loadError = signal<string | null>(null);

  /** Kontekst wejścia (`?from=`): z modułu administracyjnego wracamy do admina, inaczej do listy projektanta. */
  readonly origin = signal<DetailsOrigin>('projects');
  readonly backLabel = computed(() => this.origin() === 'projects' ? 'Wróć do listy' : 'Wróć do administracji');
  readonly isAdminContext = computed(() => this.origin() !== 'projects');

  /** Kolumny tabeli workerów (d2-grid z D2GuiShared). */
  protected readonly workerColumns: readonly GridColumn<Worker>[] = [
    { key: 'name', header: 'Element', sortable: true, filterable: true, width: '24%' },
    { key: 'workerType', header: 'Typ workera', sortable: true, filterable: true, width: '150px' },
    { key: 'configJson', header: 'Mapowanie', filterable: true }
  ];

  /** Kolumny tabeli znalezionych elementów (d2-grid). */
  protected readonly elementColumns: readonly GridColumn<DetectedElementRow>[] = [
    { key: 'name', header: 'Nazwa elementu', sortable: true },
    { key: 'label', header: 'Rodzaj', sortable: true, width: '190px' },
    { key: 'workerType', header: 'Worker', sortable: true, width: '150px' }
  ];
  protected readonly badgeClass = elementBadgeClass;
  protected readonly kindHints: Record<string, string> = CONTENT_CONTROL_KIND_HINTS;

  /** Przycisk „∨” w wierszu rozwija pełny JSON konfiguracji (szablon d2GridDetail). */
  protected readonly gridLabels: Partial<GridLabels> = {
    expandRow: 'Pokaż JSON konfiguracji',
    collapseRow: 'Ukryj JSON konfiguracji'
  };

  protected readonly trackWorker = (worker: Worker): string => worker.id;

  /**
   * Elementy znalezione w dokumencie: z `detectedElements` ostatniej wersji,
   * a gdy ich brak — wyprowadzone z konfiguracji workerów.
   */
  readonly detectedElements = computed<DetectedElementRow[]>(() => {
    const detected = this.latestVersion()?.stepData?.templateAndSchema?.detectedElements;

    if (detected) {
      return buildDetectedElementRows(detected.placeholders, detected.contentControls, detected.contentControlDetails, detected.placeholderDetails);
    }

    // Starsze wersje bez `detectedElements`: elementy wyprowadzone z konfiguracji workerów
    const placeholders: string[] = [];
    const controls: Record<string, string> = {};
    for (const worker of this.workers()) {
      try {
        const config = JSON.parse(worker.configJson) as Record<string, unknown>;
        const searchFor = readConfigKey(config, 'search_for');
        const tagName = readConfigKey(config, 'tag_name');
        if (typeof searchFor === 'string' && searchFor && !placeholders.includes(searchFor)) {
          placeholders.push(searchFor);
        }
        if (typeof tagName === 'string' && tagName) {
          controls[tagName] = worker.workerType === 'Stan' ? 'boolean' : 'array';
        }
      } catch {
        console.warn('Nie udało się odczytać konfiguracji workera:', worker.configJson);
      }
    }
    return buildDetectedElementRows(placeholders, controls);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('projectId');
    this.projectId.set(id);
    const from = this.route.snapshot.queryParamMap.get('from');
    if (from === 'admin-projects' || from === 'admin-approvals') {
      this.origin.set(from);
    }
    
    if (id) {
      this.loadProject(id);
    } else {
      this.loadError.set('Brak identyfikatora projektu w adresie.');
    }
  }

  private loadProject(id: string): void {
    this.isLoading.set(true);
    this.loadError.set(null);
    
    this.wizardDataService.getProject(id).subscribe({
      next: (project) => {
        console.log('✅ Project loaded:', project);
        this.project.set(project);
        this.isLoading.set(false);
        
        // Load versions
        this.loadVersions(id);
      },
      error: (error) => {
        console.error('❌ Error loading project:', error);
        this.loadError.set(error?.status === 404
          ? 'Projekt o tym identyfikatorze nie istnieje albo został usunięty.'
          : 'Nie udało się połączyć z serwerem albo nie masz dostępu do tego projektu.');
        this.isLoading.set(false);
      }
    });
  }

  /** Ponowna próba po błędzie. */
  retry(): void {
    const id = this.projectId();
    if (id) {
      this.loadProject(id);
    }
  }

  /** Uwagi recenzenta do kroków (w kolejności kroków) — do karty wersji. */
  reviewNotes(version: ProjectVersionResponse): Array<{ step: string; label: string; text: string }> {
    return orderedReviewNotes(version.reviewNotes);
  }

  private loadVersions(projectId: string): void {
    this.isLoadingVersions.set(true);
    
    this.wizardDataService.getProjectVersions(projectId).subscribe({
      next: (versions) => {
        console.log('✅ Versions loaded:', versions);
        this.versions.set(versions);
        
        // Find latest version (highest version number or most recent)
        if (versions && versions.length > 0) {
          const latest = versions.sort((a, b) => 
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
          )[0];
          
          this.latestVersion.set(latest);
          console.log('📋 Latest version:', latest);
          
          // Load workers for latest version
          this.loadWorkers(latest.id);
        }
        
        this.isLoadingVersions.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading versions:', error);
        this.isLoadingVersions.set(false);
      }
    });
  }

  private loadWorkers(versionId: string): void {
    this.isLoadingWorkers.set(true);
    this.workerService.getWorkersByProjectVersion(versionId).subscribe({
      next: (workers) => {
        this.workers.set(workers);
        this.isLoadingWorkers.set(false);
      },
      error: (error) => {
        console.error('❌ Error loading workers:', error);
        this.isLoadingWorkers.set(false);
      }
    });
  }

  /** Etykieta statusu wersji — API zwraca wartość enum `ProjectStatus` (liczba lub nazwa). */
  versionStatusLabel(status: number | string | null | undefined): string {
    return versionStatusLabel(status);
  }

  /** Klasa odznaki statusu (`status-pill--pending` itd.). */
  versionStatusKey(status: number | string | null | undefined): string {
    return versionStatusKey(status);
  }

  /** Powrót zależny od kontekstu wejścia: moduł administracyjny albo lista projektów projektanta. */
  goBackToList(): void {
    switch (this.origin()) {
      case 'admin-projects':
        this.router.navigate(['/admin/projects']);
        return;
      case 'admin-approvals':
        this.router.navigate(['/admin/approvals']);
        return;
      default:
        this.router.navigate(['/projects']);
    }
  }

  /** Parametry nawigacji do kreatora przenoszące kontekst (żeby „Zamknij” wróciło tutaj z tym samym kontekstem). */
  private originParams(): Record<string, string> {
    return this.origin() === 'projects' ? {} : { from: this.origin() };
  }

  startWizard(): void {
    const id = this.projectId();
    const proj = this.project();
    
    if (!id || !proj) {
      return;
    }

    console.log('🚀 Starting wizard for project:', id);

    // Try to get versions to resume from last step
    this.wizardDataService.getProjectVersions(id).subscribe({
      next: (versions) => {
        if (versions && versions.length > 0) {
          // Sort by creation date to get the latest
          const latestVersion = versions.sort((a, b) => 
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
          )[0];
          
          this.openVersionInWizard(latestVersion);
        } else {
          // No versions - start fresh
          console.log('✨ No versions found, starting new wizard');
          this.router.navigate(['/projects', id, 'wizard']);
        }
      },
      error: (error) => {
        console.error('❌ Error loading versions, starting fresh:', error);
        // On error, just start fresh wizard
        this.router.navigate(['/projects', id, 'wizard']);
      }
    });
  }

  activateVersion(versionId: string): void {
    const projectId = this.projectId();
    
    if (!projectId || !versionId) {
      return;
    }

    if (!confirm('Czy na pewno chcesz aktywować tę wersję projektu?')) {
      return;
    }

    console.log('🔄 Activating version:', versionId);

    this.wizardDataService.activateProjectVersion(projectId, versionId).subscribe({
      next: (result) => {
        console.log('✅ Version activated:', result);
        alert('Wersja została aktywowana pomyślnie');
        
        // Reload project and versions
        if (projectId) {
          this.loadProject(projectId);
        }
      },
      error: (error) => {
        console.error('❌ Error activating version:', error);
        alert('Wystąpił błąd podczas aktywacji wersji');
      }
    });
  }

  editVersion(versionId: string): void {
    const version = this.versions().find(v => v.id === versionId);
    if (!version) {
      return;
    }
    this.openVersionInWizard(version);
  }

  /**
   * Wersja robocza: kreator edytuje ją w miejscu. Wersja aktywna: najpierw kopia do nowej wersji
   * roboczej (`POST .../clone`), kreator otwiera kopię — produkcyjna konfiguracja zostaje nietknięta (BR-015).
   */
  private openVersionInWizard(version: ProjectVersionResponse): void {
    const projectId = this.projectId();
    if (!projectId) {
      return;
    }

    if (!version.isActive) {
      this.navigateToWizard(projectId, version.id);
      return;
    }

    if (!confirm(`Wersja ${version.versionTag} jest aktywna i nie będzie zmieniana.\nUtworzyć na jej podstawie nową wersję roboczą i otworzyć ją w kreatorze?`)) {
      return;
    }

    this.wizardDataService.cloneProjectVersion(projectId, version.id).subscribe({
      next: (draft) => this.navigateToWizard(projectId, draft.id),
      error: (error) => {
        console.error('Błąd tworzenia nowej wersji roboczej:', error);
        alert('Nie udało się utworzyć nowej wersji roboczej');
      }
    });
  }

  // --- paczka wdrożeniowa (BR-022) -------------------------------------------------------------------

  /** Wysyłka do aplikacji docelowej tylko dla wersji zaakceptowanej albo aktywnej (BR-017: publikacja po akceptacji). */
  canPublish(version: ProjectVersionResponse): boolean {
    const key = versionStatusKey(version.status);
    return version.isActive || key === 'approved' || key === 'active';
  }

  /** Pobiera ZIP z manifestem, wszystkimi workerami i szablonem wgranym w tej wersji. */
  downloadPackage(version: ProjectVersionResponse): void {
    const projectId = this.projectId();
    if (!projectId) {
      return;
    }
    this.packageBusyId.set(version.id);
    this.packageNotice.set(null);
    this.wizardDataService.downloadPackage(projectId, version.id, this.auth.corporateKey()).subscribe({
      next: (blob) => {
        const source = this.project()?.source ?? 'projekt';
        const fileName = `${source.replace(/[\\/:*?"<>|]/g, '_')}_v${version.versionTag}.zip`;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        a.click();
        URL.revokeObjectURL(url);
        this.packageBusyId.set(null);
        this.packageNotice.set({ kind: 'success', text: `Paczka ${fileName} została pobrana.` });
      },
      error: async (error) => {
        this.packageBusyId.set(null);
        this.packageNotice.set({ kind: 'error', text: await readBlobErrorMessage(error, 'Nie udało się zbudować paczki.') });
      }
    });
  }

  /** Buduje paczkę i wysyła do aplikacji docelowej; po sukcesie karta wersji pokazuje datę i cel. */
  publishPackage(version: ProjectVersionResponse): void {
    const projectId = this.projectId();
    if (!projectId || !this.canPublish(version)) {
      return;
    }
    if (!confirm(`Wysłać paczkę wersji ${version.versionTag} (workery + szablon) do aplikacji docelowej?`)) {
      return;
    }
    this.packageBusyId.set(version.id);
    this.packageNotice.set(null);
    this.wizardDataService.publishPackage(projectId, version.id, this.auth.getCurrentUser()?.name ?? null).subscribe({
      next: (delivery) => {
        this.packageBusyId.set(null);
        this.packageNotice.set({ kind: 'success', text: `Paczka ${delivery.fileName} (${delivery.workerCount} workerów${delivery.hasTemplate ? ', szablon' : ', bez szablonu'}) przyjęta przez ${delivery.deliveredTo}${delivery.targetResponse ? ' — ' + delivery.targetResponse : ''}.` });
        this.loadVersions(projectId);
      },
      error: (error) => {
        this.packageBusyId.set(null);
        this.packageNotice.set({ kind: 'error', text: error?.error?.message ?? 'Nie udało się wysłać paczki.' });
      }
    });
  }

  /** Podgląd wersji (także aktywnej) w kreatorze tylko do odczytu — do porównania wersji między sobą. */
  previewVersion(versionId: string): void {
    const projectId = this.projectId();
    if (!projectId) {
      return;
    }
    this.router.navigate(['/projects', projectId, 'wizard'], {
      queryParams: { versionId, mode: 'view', ...this.originParams() }
    });
  }

  private navigateToWizard(projectId: string, versionId: string): void {
    this.router.navigate(['/projects', projectId, 'wizard'], {
      queryParams: { versionId, mode: 'edit', ...this.originParams() }
    });
  }

  viewVersions(): void {
    // TODO: Implement version history view
    console.log('📚 View versions - not implemented yet');
  }

  exportProject(): void {
    // TODO: Implement project export
    console.log('💾 Export project - not implemented yet');
  }

  // Helper methods for displaying data
  getStepData(key: string): any {
    const latest = this.latestVersion();
    return latest?.stepData?.[key] || null;
  }

  getProcessData(): any {
    return this.getStepData('business') || {};
  }

  getGeneralData(): any {
    return this.getStepData('general') || {};
  }

  prettyJson(configJson: string): string {
    return prettyPrintJson(configJson);
  }

}

/** Błąd z odpowiedzi `responseType: 'blob'` przychodzi jako Blob — odczytujemy `message` z JSON-a. */
async function readBlobErrorMessage(error: { error?: unknown; message?: string }, fallback: string): Promise<string> {
  try {
    if (error?.error instanceof Blob) {
      const parsed = JSON.parse(await error.error.text()) as { message?: string };
      return parsed.message ?? fallback;
    }
    const direct = (error?.error as { message?: string } | undefined)?.message;
    return direct ?? fallback;
  } catch {
    return fallback;
  }
}
