import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
  computed,
  inject,
  signal
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Worker } from '../../models/worker.model';
import { WorkerFormData } from '../../models/worker-form.model';
import {
  WORKER_TYPE_SUBSTITUTION,
  WORKER_TYPE_VISIBILITY,
  WORKER_TYPE_REPEAT,
  parseWorkerConfigJson,
  readConfigKey
} from '../../models/worker-config.util';
import { WorkerTypeDto } from '../../data-access/external-data.model';
import { WorkerProfile, WorkerProfileService, applyProfileDefaultConfig } from '../../../admin/services/worker-profile.service';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { WorkerMappingPreviewComponent } from '../worker-mapping-preview/worker-mapping-preview.component';

interface WorkerField {
  key: string;
  label: string;
  type: 'text' | 'textarea' | 'checkbox';
  placeholder?: string;
  hint?: string;
  required?: boolean;
  /** Pole zajmuje całą szerokość siatki. */
  wide?: boolean;
  /** Mono: wyrażenia / znaczniki. */
  mono?: boolean;
}

interface WorkerTypeOption {
  code: string;
  name: string;
  description: string;
  icon: 'substitution' | 'visibility' | 'repeat' | 'generic';
}

const FALLBACK_TYPES: WorkerTypeDto[] = [
  { id: '1', code: WORKER_TYPE_SUBSTITUTION, name: WORKER_TYPE_SUBSTITUTION, isActive: true },
  { id: '2', code: WORKER_TYPE_VISIBILITY, name: WORKER_TYPE_VISIBILITY, isActive: true }
];

/**
 * Dialog „Dodaj nowe mapowanie" / „Edytuj mapowanie" (workery wersji projektu).
 *
 * Dwa tryby: **Prosty** (formularz zależny od typu workera, z podglądem mapowania na żywo i rozwijanym JSON-em)
 * i **JSON** (ręczna edycja konfiguracji). Klucze konfiguracji zapisywane są z podkreśleniami —
 * tak jak generuje je backend (`GenerateWorkersCommandHandler`, BR-010/BR-011); odczyt akceptuje też myślniki.
 * Przy nowym mapowaniu pola konfiguracji są podpowiadane z nazwy elementu, dopóki użytkownik ich sam nie zmieni.
 */
@Component({
  selector: 'app-visual-worker-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent, WorkerMappingPreviewComponent],
  templateUrl: './visual-worker-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './visual-worker-dialog.component.scss'
})
export class VisualWorkerDialogComponent implements OnInit, OnChanges {
  @Input() isOpen = false;
  @Input() worker?: Worker;
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<WorkerFormData>();

  /** Typy do wyboru = profile workerów udostępnione przez administratora (BR-016). */
  private readonly workerProfileService = inject(WorkerProfileService);
  private readonly fb = inject(FormBuilder);
  private profilesByCode = new Map<string, WorkerProfile>();
  /** Ostatni JSON wygenerowany z profilu (typ bez formularza) — żeby nie nadpisać ręcznych zmian użytkownika. */
  private lastProfileJson: string | null = null;

  readonly form: FormGroup;
  readonly workerTypes = signal<WorkerTypeDto[]>([]);
  readonly isLoadingTypes = signal(false);
  readonly selectedTypeCode = signal<string>('');
  readonly elementName = signal<string>('');

  /** `true` = tryb Prosty, `false` = tryb JSON. */
  readonly isVisualMode = signal(true);
  /** Tekst JSON: w trybie Prostym generowany z formularza, w trybie JSON edytowany ręcznie. */
  readonly jsonText = signal('{}');
  readonly jsonError = signal<string | null>(null);
  readonly showJsonPreview = signal(false);

  readonly typeOptions = computed<WorkerTypeOption[]>(() =>
    this.workerTypes().map(type => ({
      code: type.code,
      name: type.name,
      description: this.describeType(type),
      icon:
        type.code === WORKER_TYPE_SUBSTITUTION ? 'substitution' : type.code === WORKER_TYPE_VISIBILITY ? 'visibility' : type.code === WORKER_TYPE_REPEAT ? 'repeat' : 'generic'
    }))
  );

  readonly dynamicFields = computed<WorkerField[]>(() => this.getFieldsForWorkerType(this.selectedTypeCode()));

  readonly isEditMode = computed(() => !!this.worker);

  constructor() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      workerType: ['', Validators.required]
    });

    this.form.get('workerType')!.valueChanges.subscribe((code: string) => this.onWorkerTypeChange(code ?? ''));
    this.form.get('name')!.valueChanges.subscribe((name: string) => {
      this.elementName.set(name ?? '');
      this.applySuggestionsFromName();
    });
    this.form.valueChanges.subscribe(() => {
      if (this.isVisualMode()) {
        this.jsonText.set(this.buildJsonFromForm());
      }
    });
  }

  ngOnInit(): void {
    this.loadWorkerTypes();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.isVisualMode.set(true);
      this.jsonError.set(null);
      this.showJsonPreview.set(false);
      if (this.worker) {
        this.populateFormFromWorker(this.worker);
      } else {
        this.resetForm();
      }
    } else if (changes['worker'] && this.worker && this.isOpen) {
      this.populateFormFromWorker(this.worker);
    }
  }

  // ───────────────────────── dane słownikowe ─────────────────────────

  private loadWorkerTypes(): void {
    this.isLoadingTypes.set(true);
    this.workerProfileService.getVisible().subscribe({
      next: (profiles: WorkerProfile[]) => {
        this.profilesByCode = new Map(profiles.map(p => [p.key, p]));
        const types: WorkerTypeDto[] = profiles.map(p => ({
          id: p.id,
          code: p.key,
          name: p.displayName,
          description: p.description ?? undefined,
          isActive: true
        }));
        this.workerTypes.set(types.length > 0 ? types : FALLBACK_TYPES);
        this.isLoadingTypes.set(false);
        if (!this.worker && !this.form.get('workerType')?.value) {
          this.form.patchValue({ workerType: this.workerTypes()[0].code });
        }
      },
      error: () => {
        this.workerTypes.set(FALLBACK_TYPES);
        this.isLoadingTypes.set(false);
        if (!this.worker && !this.form.get('workerType')?.value) {
          this.form.patchValue({ workerType: FALLBACK_TYPES[0].code });
        }
      }
    });
  }

  private describeType(type: WorkerTypeDto): string {
    if (type.description) {
      return type.description;
    }
    switch (type.code) {
      case WORKER_TYPE_SUBSTITUTION:
        return 'Podmień znacznik <%…%> na wartość z modelu danych';
      case WORKER_TYPE_VISIBILITY:
        return 'Pokaż lub ukryj kontrolkę tekstową zależnie od warunku';
      case WORKER_TYPE_REPEAT:
        return 'Powiel fragment (sekcję, pozycję listy, wiersz tabeli) dla każdego elementu listy z danych';
      default:
        return 'Konfiguracja zależna od typu';
    }
  }

  // ───────────────────────── pola zależne od typu ─────────────────────────

  private getFieldsForWorkerType(code: string): WorkerField[] {
    switch (code) {
      case WORKER_TYPE_SUBSTITUTION:
        return [
          {
            key: 'search_for',
            label: 'Znacznik w dokumencie',
            type: 'text',
            placeholder: '<%data_aktualna%>',
            hint: 'Dokładny tekst placeholdera w szablonie Word',
            required: true,
            mono: true
          },
          {
            key: 'replacement_property_expression',
            label: 'Wartość z modelu danych',
            type: 'text',
            placeholder: 'formModel.data_aktualna',
            hint: 'Ścieżka do pola w danych, np. formModel.adres.miasto',
            required: true,
            mono: true
          },
          {
            key: 'default_value',
            label: 'Wartość domyślna',
            type: 'text',
            placeholder: 'np. BRAK DANYCH',
            hint: 'Wstawiana, gdy w danych nie ma wartości',
            required: false
          }
        ];
      case WORKER_TYPE_VISIBILITY:
        return [
          {
            key: 'tag_name',
            label: 'Nazwa kontrolki tekstowej',
            type: 'text',
            placeholder: 'tag_pokaz_regulamin',
            hint: 'Tag kontrolki (content control) w szablonie Word',
            required: true,
            mono: true
          },
          {
            key: 'conditional_expression',
            label: 'Warunek widoczności',
            type: 'textarea',
            placeholder: 'formModel.tag_pokaz_regulamin',
            hint: 'Wyrażenie, które musi być prawdziwe, aby element był widoczny',
            required: true,
            wide: true,
            mono: true
          },
          {
            key: 'hide_if_false',
            label: 'Ukryj element, gdy warunek jest fałszywy',
            type: 'checkbox',
            required: false,
            wide: true
          }
        ];
      case WORKER_TYPE_REPEAT:
        return [
          {
            key: 'tag_name',
            label: 'Nazwa formantu powielanego',
            type: 'text',
            placeholder: 'pozycje_faktury',
            hint: 'Tag sekcji powtarzanej, listy albo wiersza tabeli w szablonie Word',
            required: true,
            mono: true
          },
          {
            key: 'collection_expression',
            label: 'Lista z modelu danych',
            type: 'text',
            placeholder: 'formModel.pozycje',
            hint: 'Ścieżka do listy; fragment zostanie powielony dla każdego elementu',
            required: true,
            mono: true
          },
          {
            key: 'repeat_scope',
            label: 'Zakres powielania',
            type: 'text',
            placeholder: 'block | paragraph | row',
            hint: 'block = cały blok, paragraph = akapit (pozycja listy), row = wiersz tabeli',
            required: false,
            mono: true
          }
        ];
      default:
        return [];
    }
  }

  private onWorkerTypeChange(code: string): void {
    this.selectedTypeCode.set(code);

    Object.keys(this.form.controls)
      .filter(key => key !== 'name' && key !== 'workerType')
      .forEach(key => this.form.removeControl(key, { emitEvent: false }));

    for (const field of this.getFieldsForWorkerType(code)) {
      const initial = field.type === 'checkbox' ? false : '';
      this.form.addControl(field.key, this.fb.control(initial, field.required ? [Validators.required] : []), {
        emitEvent: false
      });
    }

    this.applySuggestionsFromName();
    if (!this.applyProfileDefault(code)) {
      this.jsonText.set(this.buildJsonFromForm());
    }
  }

  /**
   * Typ bez formularza Prostego (profil z katalogu, np. „Usuń obszar”): nowe mapowanie startuje w trybie JSON
   * z domyślną konfiguracją profilu (token `{name}` → nazwa elementu). Zwraca `true`, gdy zastosowano.
   */
  private applyProfileDefault(code: string): boolean {
    if (this.worker || this.getFieldsForWorkerType(code).length > 0) {
      this.lastProfileJson = null;
      return false;
    }
    const profile = this.profilesByCode.get(code);
    if (!profile) {
      return false;
    }
    const generated = applyProfileDefaultConfig(profile, this.elementName());
    // nie nadpisuj, jeśli użytkownik już ręcznie zmienił JSON
    if (this.lastProfileJson !== null && this.jsonText() !== this.lastProfileJson) {
      return true;
    }
    this.lastProfileJson = generated;
    this.jsonText.set(generated);
    this.jsonError.set(null);
    this.isVisualMode.set(false);
    return true;
  }

  /** Nowe mapowanie: podpowiedz konfigurację z nazwy elementu, dopóki użytkownik sam nie zmienił pola. */
  private applySuggestionsFromName(): void {
    if (this.worker) {
      return;
    }
    const name = (this.form.get('name')?.value ?? '').toString().trim();
    const suggestions: Record<string, string> =
      this.selectedTypeCode() === WORKER_TYPE_SUBSTITUTION
        ? { search_for: name ? `<%${name}%>` : '', replacement_property_expression: name ? `formModel.${name}` : '' }
        : this.selectedTypeCode() === WORKER_TYPE_VISIBILITY
          ? { tag_name: name, conditional_expression: name ? `formModel.${name}` : '' }
          : {};

    for (const [key, value] of Object.entries(suggestions)) {
      const control = this.form.get(key);
      if (control && control.pristine) {
        control.setValue(value, { emitEvent: false });
      }
    }
    if (!this.applyProfileDefault(this.selectedTypeCode())) {
      this.jsonText.set(this.buildJsonFromForm());
    }
  }

  private populateFormFromWorker(worker: Worker): void {
    this.form.reset({}, { emitEvent: false });
    this.form.patchValue({ name: worker.name }, { emitEvent: false });
    this.elementName.set(worker.name);
    this.form.patchValue({ workerType: worker.workerType }); // emituje → onWorkerTypeChange buduje pola

    const config = parseWorkerConfigJson(worker.configJson) ?? {};
    for (const field of this.getFieldsForWorkerType(worker.workerType)) {
      const control = this.form.get(field.key);
      if (!control) {
        continue;
      }
      const value = readConfigKey(config, field.key);
      control.setValue(field.type === 'checkbox' ? value === true || value === 'true' : (value ?? ''), {
        emitEvent: false
      });
      control.markAsDirty();
    }

    // Nieznany typ albo klucze spoza formularza: pokaż JSON jaki jest, żeby nic nie zgubić przy zapisie.
    const knownKeys = new Set(this.getFieldsForWorkerType(worker.workerType).map(f => f.key));
    const hasUnknownKeys = Object.keys(config).some(k => !knownKeys.has(k) && !knownKeys.has(k.replace(/-/g, '_')));
    if (!this.getFieldsForWorkerType(worker.workerType).length || hasUnknownKeys) {
      this.isVisualMode.set(false);
      this.jsonText.set(worker.configJson || '{}');
    } else {
      this.jsonText.set(this.buildJsonFromForm());
    }
  }

  private resetForm(): void {
    this.form.reset({}, { emitEvent: false });
    this.elementName.set('');
    this.lastProfileJson = null;
    const defaultType = this.workerTypes()[0]?.code ?? '';
    this.form.patchValue({ workerType: defaultType }); // emituje → onWorkerTypeChange
    this.jsonText.set(this.buildJsonFromForm());
  }

  // ───────────────────────── JSON ─────────────────────────

  private buildJsonFromForm(): string {
    const config: Record<string, unknown> = {};
    const value = this.form.value as Record<string, unknown>;
    for (const field of this.dynamicFields()) {
      const raw = value[field.key];
      if (field.type === 'checkbox') {
        if (raw === true) {
          config[field.key] = true;
        }
      } else if (raw !== null && raw !== undefined && String(raw).trim() !== '') {
        config[field.key] = raw;
      }
    }
    return JSON.stringify(config, null, 2);
  }

  /** Tryb JSON → Prosty: przepisz znane klucze do pól formularza. */
  private syncFormFromJson(): boolean {
    const config = parseWorkerConfigJson(this.jsonText());
    if (!config) {
      this.jsonError.set('Nieprawidłowy format JSON');
      return false;
    }
    this.jsonError.set(null);
    for (const field of this.dynamicFields()) {
      const control = this.form.get(field.key);
      if (!control) {
        continue;
      }
      const value = readConfigKey(config, field.key);
      control.setValue(field.type === 'checkbox' ? value === true || value === 'true' : (value ?? ''), {
        emitEvent: false
      });
      control.markAsDirty();
    }
    this.jsonText.set(this.buildJsonFromForm());
    return true;
  }

  setMode(visual: boolean): void {
    if (visual === this.isVisualMode()) {
      return;
    }
    if (visual) {
      if (!this.syncFormFromJson()) {
        return; // zostań w trybie JSON, dopóki JSON jest zepsuty
      }
    } else {
      this.jsonText.set(this.buildJsonFromForm());
    }
    this.isVisualMode.set(visual);
  }

  onJsonChange(value: string): void {
    this.jsonText.set(value);
    this.jsonError.set(parseWorkerConfigJson(value) ? null : 'Nieprawidłowy format JSON');
  }

  toggleJsonPreview(): void {
    this.showJsonPreview.update(v => !v);
  }

  // ───────────────────────── akcje ─────────────────────────

  selectType(code: string): void {
    if (this.form.get('workerType')?.value !== code) {
      this.form.patchValue({ workerType: code });
    }
  }

  onCancel(): void {
    this.close.emit();
  }

  get canSave(): boolean {
    const name = (this.form.get('name')?.value ?? '').toString().trim();
    const type = this.form.get('workerType')?.value;
    if (!name || !type) {
      return false;
    }
    return this.isVisualMode() ? this.form.valid : !this.jsonError() && !!parseWorkerConfigJson(this.jsonText());
  }

  onSave(): void {
    if (this.isVisualMode()) {
      if (this.form.invalid) {
        this.form.markAllAsTouched();
        return;
      }
      this.jsonText.set(this.buildJsonFromForm());
    } else if (!parseWorkerConfigJson(this.jsonText())) {
      this.jsonError.set('Nieprawidłowy format JSON');
      return;
    }

    const value = this.form.value;
    this.save.emit({
      name: String(value.name ?? '').trim(),
      workerType: value.workerType,
      description: this.jsonText(), // JSON konfiguracji (pole `description` w WorkerFormData)
      defaultValue: ''
    });
  }

  controlHasError(key: string): boolean {
    const control = this.form.get(key);
    return !!control && control.invalid && (control.touched || control.dirty);
  }
}
