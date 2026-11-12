import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnInit, Output, computed, inject, input, signal } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { D2GridCellDef, D2GridComponent, D2GridDetailDef, GridColumn, GridLabels } from 'd2-gui-shared';
import { WorkerService } from '../../services/worker.service';
import type { Worker } from '../../models/worker.model';
import { WorkerFormData } from '../../models/worker-form.model';
import { LetterWizardFacade } from '../../data-access/letter-wizard.facade';
import { VisualWorkerDialogComponent } from '../../components/visual-worker-dialog/visual-worker-dialog.component';
import { WorkerMappingPreviewComponent } from '../../components/worker-mapping-preview/worker-mapping-preview.component';
import { prettyPrintJson } from '../../models/worker-config.util';

/**
 * Krok 3 kreatora: lista workerów (mapowań) wersji projektu.
 * Tabela to `d2-grid` z D2GuiShared — wyszukiwanie, filtry kolumnowe, sortowanie
 * i stronicowanie są w kontrolce; komponent odpowiada za dane i dialog edycji.
 */
@Component({
  selector: 'app-configuration-step',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    D2GridComponent,
    D2GridCellDef,
    D2GridDetailDef,
    VisualWorkerDialogComponent,
    WorkerMappingPreviewComponent
  ],
  templateUrl: './configuration-step.component.html',
  styleUrl: './configuration-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfigurationStepComponent implements OnInit {
  @Input() group!: FormGroup;
  /** Podgląd: bez dodawania, edycji i usuwania workerów. */
  readonly readOnly = input(false);
  /** Krok gotowy: workery wczytane (albo brak wersji / błąd). */
  @Output() ready = new EventEmitter<void>();

  private readonly workerService = inject(WorkerService);
  private readonly facade = inject(LetterWizardFacade);

  isLoadingWorkers = signal(false);
  isDialogOpen = signal(false);
  editingWorker = signal<Worker | undefined>(undefined);
  workers = signal<Worker[]>([]);

  /** Kolumny tabeli; filtr kolumny „Mapowanie” przeszukuje surowy JSON konfiguracji. W podglądzie bez akcji. */
  protected readonly columns = computed<readonly GridColumn<Worker>[]>(() => {
    const base: GridColumn<Worker>[] = [
      { key: 'name', header: 'Element', sortable: true, filterable: true, width: '22%' },
      { key: 'workerType', header: 'Typ workera', sortable: true, filterable: true, width: '150px' },
      { key: 'configJson', header: 'Mapowanie', filterable: true }
    ];
    if (!this.readOnly()) {
      base.push({ key: 'actions', header: 'Akcje', headerHidden: true, searchable: false, width: '130px', align: 'right' });
    }
    return base;
  });

  /** Przycisk „∨” w wierszu rozwija pełny JSON konfiguracji (szablon d2GridDetail). */
  protected readonly gridLabels: Partial<GridLabels> = {
    expandRow: 'Pokaż JSON konfiguracji',
    collapseRow: 'Ukryj JSON konfiguracji'
  };

  protected readonly trackWorker = (worker: Worker): string => worker.id;

  ngOnInit(): void {
    this.loadWorkers();
  }

  private loadWorkers(): void {
    const versionId = this.facade.getCreatedVersionId();
    if (!versionId) {
      console.warn('Brak identyfikatora wersji — nie można wczytać workerów');
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
        console.error('Błąd wczytywania workerów:', error);
        this.isLoadingWorkers.set(false);
        this.ready.emit();
      }
    });
  }

  prettyJson(configJson: string): string {
    return prettyPrintJson(configJson);
  }

  addWorker(): void {
    this.editingWorker.set(undefined);
    this.isDialogOpen.set(true);
  }

  editWorker(worker: Worker): void {
    this.editingWorker.set(worker);
    this.isDialogOpen.set(true);
  }

  deleteWorker(worker: Worker): void {
    if (!confirm(`Czy na pewno chcesz usunąć worker "${worker.name}"?`)) {
      return;
    }

    this.workerService.deleteWorker(worker.id).subscribe({
      next: () => this.loadWorkers(),
      error: (error) => {
        console.error('Błąd usuwania workera:', error);
        alert('Wystąpił błąd podczas usuwania workera');
      }
    });
  }

  closeDialog(): void {
    this.isDialogOpen.set(false);
    this.editingWorker.set(undefined);
  }

  saveWorker(formData: WorkerFormData): void {
    const versionId = this.facade.getCreatedVersionId();
    if (!versionId) {
      console.error('Brak identyfikatora wersji — nie można zapisać workera');
      return;
    }

    const editingWorker = this.editingWorker();
    const request$ = editingWorker
      ? this.workerService.updateWorker(editingWorker.id, formData)
      : this.workerService.createWorker(versionId, formData);

    request$.subscribe({
      next: () => {
        this.closeDialog();
        this.loadWorkers();
      },
      error: (error) => {
        console.error(editingWorker ? 'Błąd aktualizacji workera:' : 'Błąd tworzenia workera:', error);
        alert(editingWorker ? 'Wystąpił błąd podczas aktualizacji workera' : 'Wystąpił błąd podczas tworzenia workera');
      }
    });
  }
}
