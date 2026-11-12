
import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output, signal, inject, computed } from '@angular/core';
import { D2GridCellDef, D2GridComponent, D2GridDetailDef, GridColumn, GridLabels } from '../../../../shared/ui/grid';
import { LetterWizardData, LetterWizardStepKey } from '../../data-access/letter-wizard.model';
import { SubmissionIssue } from '../../data-access/letter-wizard-submission';
import { RecipientDto, BusinessLineDto, StatusDto } from '../../data-access/external-data.model';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { WorkerService } from '../../services/worker.service';
import { LetterWizardFacade } from '../../data-access/letter-wizard.facade';
import type { Worker } from '../../models/worker.model';
import { WorkerMappingPreviewComponent } from '../../components/worker-mapping-preview/worker-mapping-preview.component';
import { prettyPrintJson } from '../../models/worker-config.util';
import { CONTENT_CONTROL_KIND_HINTS, DetectedElementRow, buildDetectedElementRows, elementBadgeClass } from '../../models/content-control-kind';
import { TenantService } from '../../../tenants/services/tenant.service';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-summary-step',
  standalone: true,
  imports: [ButtonComponent, WorkerMappingPreviewComponent, D2GridComponent, D2GridCellDef, D2GridDetailDef],
  templateUrl: './summary-step.component.html',
  styleUrls: ['./summary-step.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SummaryStepComponent implements OnInit {
  @Input() data!: LetterWizardData;
  @Input() recipients: RecipientDto[] = [];
  @Input() businessLines: BusinessLineDto[] = [];
  @Input() statuses: StatusDto[] = [];
  /** Braki blokujące „Wyślij do weryfikacji" — liczone w LetterWizardPageComponent (letter-wizard-submission.ts). */
  @Input() submissionIssues: SubmissionIssue[] = [];
  /** Podgląd: bez akcji (przejście do uzupełnienia danych, test generowania). */
  @Input() readOnly = false;
  @Output() goToStep = new EventEmitter<LetterWizardStepKey>();
  /** Krok gotowy: workery wczytane (albo brak wersji / błąd). */
  @Output() ready = new EventEmitter<void>();

  private readonly workerService = inject(WorkerService);
  private readonly facade = inject(LetterWizardFacade);
  private readonly tenantService = inject(TenantService);
  private readonly auth = inject(AuthService);
  
  workers = signal<Worker[]>([]);
  /** Nazwa wybranego zespołu (identyfikator → nazwa z listy zespołów użytkownika). */
  private readonly tenantNames = signal<Record<string, string>>({});
  readonly tenantName = computed(() => {
    const id = this.data?.business?.tenant;
    if (!id) {
      return 'projekt prywatny';
    }
    return this.tenantNames()[id] ?? id;
  });
  isLoadingWorkers = signal(false);

  /** Kolumny tabeli workerów (d2-grid). */
  protected readonly workerColumns: readonly GridColumn<Worker>[] = [
    { key: 'name', header: 'Element', sortable: true, width: '24%' },
    { key: 'workerType', header: 'Typ workera', sortable: true, width: '150px' },
    { key: 'configJson', header: 'Mapowanie' }
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

  /** Elementy z analizy szablonu: z tej sesji (upload) albo zapisane w wersji (`detectedElements`). */
  detectedElements = computed<DetectedElementRow[]>(() => {
    const result = this.facade.getPlaceholderExtractionResult()
      ?? this.facade.state()?.data.templateAndSchema?.detectedElements;
    if (!result) return [];
    return buildDetectedElementRows(result.placeholders, result.contentControls, result.contentControlDetails, result.placeholderDetails);
  });

  prettyJson(configJson: string): string {
    return prettyPrintJson(configJson);
  }

  isMissing(field: string): boolean {
    return this.submissionIssues.some(issue => issue.field === field);
  }

  onFixIssues(): void {
    this.goToStep.emit(this.submissionIssues[0]?.step ?? 'documentDescription');
  }

  ngOnInit(): void {
    if (this.data?.business?.tenant) {
      this.tenantService.getTenants(this.auth.corporateKey()).subscribe({
        next: (tenants) => this.tenantNames.set(Object.fromEntries(tenants.map((t) => [t.id, t.name]))),
        error: () => undefined
      });
    }
    this.loadWorkers();
  }

  private loadWorkers(): void {
    const versionId = this.facade.getCreatedVersionId();
    if (!versionId) {
      console.warn('⚠️ No version ID available, cannot load workers');
      this.ready.emit();
      return;
    }

    this.isLoadingWorkers.set(true);
    this.workerService.getWorkersByProjectVersion(versionId).subscribe({
      next: (workers) => {
        this.workers.set(workers);
        this.isLoadingWorkers.set(false);
        this.ready.emit();
      },
      error: (error) => {
        console.error('❌ Error loading workers:', error);
        this.isLoadingWorkers.set(false);
        this.ready.emit();
      }
    });
  }

  getEnvironmentsText(): string {
    const environments = this.data.general?.environments;
    if (!environments || environments.length === 0) {
      return '-';
    }
    
    const envMap: Record<string, string> = {
      'dev': 'DEV',
      'test': 'TEST',
      'uat': 'UAT',
      'prod': 'PROD'
    };
    
    return environments.map(env => envMap[env] || env.toUpperCase()).join(', ');
  }
  
  getBusinessLineText(): string {
    const code = this.data.business?.businessLine;
    if (!code) return '-';
    
    const businessLine = this.businessLines.find(bl => bl.code === code);
    return businessLine?.name || code;
  }
  
  getRecipientText(): string {
    const code = this.data.general?.recipient;
    if (!code) return '-';
    
    const recipient = this.recipients.find(r => r.code === code);
    return recipient?.name || code;
  }
  
  getStatusText(): string {
    const code = this.data.business?.status;
    if (!code) return '-';
    
    const status = this.statuses.find(s => s.code === code);
    return status?.name || code;
  }
  
  getVisibilityScopeText(): string {
    const scope = this.data.configuration?.visibilityScope;
    if (!scope) return '-';
    
    const scopeMap: Record<string, string> = {
      'internal': 'Wewnętrzny',
      'external': 'Zewnętrzny',
      'public': 'Publiczny'
    };
    
    return scopeMap[scope] || scope;
  }
  
  getTemplateText(): string {
    const templateId = this.data.templateAndSchema?.templateId;
    if (!templateId) return '-';
    
    // Jeśli to GUID, wyświetl "Szablon wczytany"
    const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    if (guidPattern.test(templateId)) {
      return 'Szablon wczytany z pliku';
    }
    
    return templateId;
  }

}
