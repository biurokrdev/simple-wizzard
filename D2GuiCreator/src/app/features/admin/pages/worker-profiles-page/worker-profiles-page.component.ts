import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { D2GridCellDef, D2GridComponent, D2GridDetailDef, GridColumn, GridLabels } from '../../../../shared/ui/grid';
import { PageLayoutComponent } from '../../../../shared/ui/page-layout/page-layout.component';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { prettyPrintJson } from '../../../letter-wizard/models/worker-config.util';
import {
  WorkerProfile,
  WorkerProfileService,
  WorkerProfileSyncResult,
} from '../../services/worker-profile.service';

/**
 * Administracja → Profile workerów (BR-016): lista typów z katalogu aplikacji docelowej, decyzja
 * „widoczny w kreatorze”, edycja opisu i domyślnej konfiguracji JSON, synchronizacja z katalogiem,
 * ręczne dodawanie profili.
 */
@Component({
  selector: 'app-worker-profiles-page',
  standalone: true,
  imports: [DatePipe, ReactiveFormsModule, PageLayoutComponent, ButtonComponent, D2GridComponent, D2GridCellDef, D2GridDetailDef],
  templateUrl: './worker-profiles-page.component.html',
  styleUrl: './worker-profiles-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkerProfilesPageComponent implements OnInit {
  private readonly service = inject(WorkerProfileService);
  private readonly fb = inject(FormBuilder);

  readonly profiles = signal<WorkerProfile[]>([]);
  readonly isLoading = signal(false);
  readonly isSyncing = signal(false);
  readonly syncResult = signal<WorkerProfileSyncResult | null>(null);
  readonly error = signal<string | null>(null);
  readonly togglingId = signal<string | null>(null);

  /** Dialog edycji/dodawania. */
  readonly editing = signal<WorkerProfile | null>(null);
  readonly isDialogOpen = signal(false);
  readonly isSaving = signal(false);
  readonly jsonError = signal<string | null>(null);

  readonly visibleCount = computed(() => this.profiles().filter((p) => p.isVisible).length);
  readonly missingCount = computed(() => this.profiles().filter((p) => p.isMissingInSource).length);

  readonly form = this.fb.nonNullable.group({
    key: ['', [Validators.required, Validators.maxLength(100)]],
    displayName: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    defaultConfigJson: ['{}'],
    isVisible: [false],
    sortOrder: [0],
  });

  protected readonly columns: readonly GridColumn<WorkerProfile>[] = [
    { key: 'isVisible', header: 'W kreatorze', width: '120px', align: 'center', sortable: true, searchable: false,
      value: (p) => (p.isVisible ? 1 : 0), format: (v) => (v ? 'tak' : 'nie') },
    { key: 'key', header: 'Typ workera', sortable: true, filterable: true },
    { key: 'displayName', header: 'Nazwa', sortable: true, filterable: true },
    { key: 'description', header: 'Opis', filterable: true },
    { key: 'source', header: 'Źródło', sortable: true, width: '150px',
      value: (p) => (p.isMissingInSource ? 'brak w katalogu' : p.source === 'Manual' ? 'ręczny' : 'katalog') },
    { key: 'lastSyncedAt', header: 'Synchronizacja', sortable: true, width: '150px', searchable: false },
    { key: 'actions', header: 'Akcje', headerHidden: true, searchable: false, width: '150px', align: 'right' },
  ];

  protected readonly gridLabels: Partial<GridLabels> = {
    expandRow: 'Pokaż domyślną konfigurację JSON',
    collapseRow: 'Ukryj domyślną konfigurację JSON',
    empty: 'Brak profili — uruchom synchronizację z aplikacją docelową albo dodaj profil ręcznie',
  };

  protected readonly trackProfile = (p: WorkerProfile): string => p.id;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.service.getAll().subscribe({
      next: (profiles) => {
        this.profiles.set(profiles);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Błąd wczytywania profili workerów:', err);
        this.error.set('Nie udało się wczytać profili workerów.');
        this.isLoading.set(false);
      },
    });
  }

  sync(): void {
    this.isSyncing.set(true);
    this.error.set(null);
    this.service.sync().subscribe({
      next: (result) => {
        this.syncResult.set(result);
        this.isSyncing.set(false);
        this.load();
      },
      error: (err) => {
        console.error('Błąd synchronizacji profili:', err);
        this.error.set(err?.error?.message ?? 'Synchronizacja nie powiodła się — katalog aplikacji docelowej jest niedostępny.');
        this.isSyncing.set(false);
      },
    });
  }

  /** Szybkie przełączenie widoczności z listy (reszta pól bez zmian). */
  toggleVisible(profile: WorkerProfile): void {
    this.togglingId.set(profile.id);
    this.service
      .update(profile.id, {
        displayName: profile.displayName,
        description: profile.description,
        defaultConfigJson: profile.defaultConfigJson,
        isVisible: !profile.isVisible,
        sortOrder: profile.sortOrder,
      })
      .subscribe({
        next: (updated) => {
          this.profiles.update((list) => list.map((p) => (p.id === updated.id ? updated : p)));
          this.togglingId.set(null);
        },
        error: (err) => {
          console.error('Błąd zmiany widoczności:', err);
          this.error.set('Nie udało się zmienić widoczności profilu.');
          this.togglingId.set(null);
        },
      });
  }

  openCreate(): void {
    this.editing.set(null);
    this.form.reset({ key: '', displayName: '', description: '', defaultConfigJson: '{\n  \n}', isVisible: false, sortOrder: (this.maxSortOrder() + 10) });
    this.form.controls.key.enable();
    this.jsonError.set(null);
    this.isDialogOpen.set(true);
  }

  openEdit(profile: WorkerProfile): void {
    this.editing.set(profile);
    this.form.reset({
      key: profile.key,
      displayName: profile.displayName,
      description: profile.description ?? '',
      defaultConfigJson: prettyPrintJson(profile.defaultConfigJson),
      isVisible: profile.isVisible,
      sortOrder: profile.sortOrder,
    });
    this.form.controls.key.disable();
    this.jsonError.set(null);
    this.isDialogOpen.set(true);
  }

  closeDialog(): void {
    this.isDialogOpen.set(false);
    this.editing.set(null);
  }

  onJsonInput(): void {
    const text = this.form.controls.defaultConfigJson.value;
    this.jsonError.set(validateJsonObject(text));
  }

  save(): void {
    this.form.markAllAsTouched();
    const jsonProblem = validateJsonObject(this.form.controls.defaultConfigJson.value);
    this.jsonError.set(jsonProblem);
    if (this.form.invalid || jsonProblem) {
      return;
    }

    const raw = this.form.getRawValue();
    const payload = {
      displayName: raw.displayName.trim(),
      description: raw.description.trim() || null,
      defaultConfigJson: JSON.stringify(JSON.parse(raw.defaultConfigJson || '{}')),
      isVisible: raw.isVisible,
      sortOrder: Number(raw.sortOrder) || 0,
    };
    this.isSaving.set(true);
    const editing = this.editing();
    const request$ = editing
      ? this.service.update(editing.id, payload)
      : this.service.create({ key: raw.key.trim(), ...payload });

    request$.subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeDialog();
        this.load();
      },
      error: (err) => {
        console.error('Błąd zapisu profilu:', err);
        this.isSaving.set(false);
        this.error.set(err?.error?.message ?? 'Nie udało się zapisać profilu.');
      },
    });
  }

  remove(profile: WorkerProfile): void {
    if (profile.source !== 'Manual') {
      return;
    }
    if (!confirm(`Usunąć ręcznie dodany profil „${profile.displayName}”?`)) {
      return;
    }
    this.service.delete(profile.id).subscribe({
      next: () => this.load(),
      error: (err) => {
        console.error('Błąd usuwania profilu:', err);
        this.error.set(err?.error?.message ?? 'Nie udało się usunąć profilu.');
      },
    });
  }

  prettyJson(json: string): string {
    return prettyPrintJson(json);
  }

  private maxSortOrder(): number {
    return this.profiles().reduce((max, p) => Math.max(max, p.sortOrder), 0);
  }
}

/** `null` gdy tekst jest obiektem JSON (lub pusty), inaczej komunikat. */
function validateJsonObject(text: string): string | null {
  if (!text.trim()) {
    return null;
  }
  try {
    const parsed = JSON.parse(text);
    return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? null : 'Konfiguracja musi być obiektem JSON ({ … }).';
  } catch (e) {
    return `Niepoprawny JSON: ${(e as Error).message}`;
  }
}
