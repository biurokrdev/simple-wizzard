
import { afterNextRender, ChangeDetectionStrategy, Component, EventEmitter, Input, Output, signal, inject, OnChanges, SimpleChanges, ViewChild, ElementRef } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ValidationNotesComponent, ValidationNote } from '../../components/validation-notes/validation-notes.component';
import { DocumentService, DocumentValidationResult, PlaceholderExtractionResult } from '../../services/document.service';
import { forkJoin } from 'rxjs';
import { D2GridCellDef, D2GridComponent, GridColumn } from 'd2-gui-shared';
import { LetterWizardFacade } from '../../data-access/letter-wizard.facade';
import {
  CONTENT_CONTROL_KIND_HINTS,
  DetectedElementRow,
  buildDetectedElementRows,
  elementBadgeClass,
  summarizeContentControlKinds
} from '../../models/content-control-kind';

// Słownik opisów kodów błędów i ostrzeżeń
const ERROR_MESSAGES: Record<string, string> = {
  'INVALID_PLACEHOLDER': 'Znaleziono placeholder w nieprawidłowym formacie. Upewnij się, że wszystkie placeholdery są w formacie <%nazwa_zmiennej%>.',
  'MISSING_CLOSING_TAG': 'W dokumencie brakuje zamykającego znacznika dla elementu content control. Sprawdź strukturę dokumentu.',
  'EMPTY_FILE': 'Przesłany plik jest pusty lub uszkodzony.',
  'CORRUPTED_DOCUMENT': 'Struktura dokumentu jest uszkodzona. Spróbuj otworzyć plik w Microsoft Word i zapisać ponownie.',
  'UNSUPPORTED_FORMAT': 'Format pliku nie jest obsługiwany. Dozwolone są pliki .docx i .xlsx.',
  'MISSING_REQUIRED_SECTION': 'Brakuje wymaganej sekcji w dokumencie.',
  'DUPLICATE_PLACEHOLDER': 'W dokumencie występują zduplikowane placeholdery o tej samej nazwie.',
  'INVALID_CONTENT_CONTROL': 'Znaleziono content control o nieprawidłowej konfiguracji.',
};

const WARNING_MESSAGES: Record<string, string> = {
  'DEPRECATED_SYNTAX': 'Dokument używa przestarzałej składni. Zalecane jest zaktualizowanie szablonu do nowej wersji.',
  'UNKNOWN_CONTENT_CONTROL': 'Znaleziono niestandardowy typ content control, który może nie być poprawnie obsługiwany.',
  'MISSING_METADATA': 'Brakuje niektórych metadanych w dokumencie.',
  'LARGE_FILE_SIZE': 'Plik jest bardzo duży, co może wpłynąć na wydajność przetwarzania.',
  'UNUSUAL_FORMATTING': 'Wykryto nietypowe formatowanie, które może być niezamierzone.',
};

/** Szablon: dokument Word (.docx) albo arkusz Excel (.xlsx, od 2026-11-08). */
export function isSupportedTemplateFile(name: string): boolean {
  const lower = name.toLowerCase();
  return lower.endsWith('.docx') || lower.endsWith('.xlsx');
}

@Component({
  selector: 'app-template-and-schema-step',
  standalone: true,
  imports: [ReactiveFormsModule, ValidationNotesComponent, D2GridComponent, D2GridCellDef],
  templateUrl: './template-and-schema-step.component.html',
  styleUrls: ['./template-and-schema-step.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TemplateAndSchemaStepComponent implements OnChanges {
  @Input() group!: FormGroup;
  /** Podgląd: bez uploadu, przeciągania i usuwania pliku. */
  @Input() readOnly = false;
  @Input() generalGroup?: FormGroup;
  @Input() initialDataSchema?: string | null;

  @Output() instructionRequested = new EventEmitter<void>();
  /** Krok gotowy po pierwszym renderze (nie ma danych do dociągnięcia). */
  @Output() ready = new EventEmitter<void>();
  @Output() placeholdersExtracted = new EventEmitter<PlaceholderExtractionResult>();
  
  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;

  private readonly documentService = inject(DocumentService);
  private readonly facade = inject(LetterWizardFacade);

  /**
   * Znalezione elementy z rodzajem: znaczniki `<%…%>` oraz formanty jako wariant / powtarzany / lista / tabela.
   * Po wczytaniu pliku — z odpowiedzi API; przy edycji / podglądzie — z `detectedElements` zapisanych w wersji.
   */
  readonly detectedRows = signal<DetectedElementRow[]>([]);
  protected readonly elementColumns: readonly GridColumn<DetectedElementRow>[] = [
    { key: 'name', header: 'Element', sortable: true, filterable: true, width: '26%' },
    { key: 'label', header: 'Rodzaj', sortable: true, filterable: true, width: '190px' },
    { key: 'location', header: 'Miejsce', sortable: true, width: '120px', searchable: false },
    { key: 'preview', header: 'Treść', filterable: true, collapsible: true, value: (r) => r.preview ?? '' },
    { key: 'workerType', header: 'Worker', sortable: true, width: '150px' }
  ];
  protected readonly trackRow = (r: DetectedElementRow): string => `${r.type}:${r.name}`;
  protected readonly kindHints: Record<string, string> = CONTENT_CONTROL_KIND_HINTS;
  protected readonly badgeClass = elementBadgeClass;

  readonly isDragging = signal(false);
  readonly isUploading = signal(false);
  readonly isValidating = signal(false);
  readonly isExtractingPlaceholders = signal(false);
  readonly isShowingApiRequest = signal(false);
  
  // Przechowywanie pliku i wyników
  private uploadedFile: File | null = null;
  private extractedPlaceholders: string[] = [];
  validationNotes: ValidationNote[] = [];
  generatedSchema: Record<string, any> | null = null;
  apiRequestExample: string = '';

  get templateIdControl() {
    return this.group.get('templateId');
  }

  get dataSchemaIdControl() {
    return this.group.get('dataSchemaId');
  }

  constructor() {
    afterNextRender(() => this.ready.emit());
    this.restoreDetectedRows();
  }

  /** Edycja / podgląd: elementy z poprzedniej analizy szablonu zapisane w `step_data`. */
  private restoreDetectedRows(): void {
    const fromUpload = this.facade.getPlaceholderExtractionResult();
    const saved = this.facade.state()?.data.templateAndSchema?.detectedElements;
    const source = fromUpload ?? saved;
    if (source) {
      this.detectedRows.set(buildDetectedElementRows(source.placeholders, source.contentControls, source.contentControlDetails, source.placeholderDetails));
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    // When initialDataSchema is provided (edit mode), restore the schema
    if (changes['initialDataSchema'] && this.initialDataSchema) {
      console.log('📥 Restoring schema from saved state:', this.initialDataSchema.substring(0, 100) + '...');
      
      try {
        // Parse the saved schema JSON
        const parsedSchema = JSON.parse(this.initialDataSchema);
        this.generatedSchema = parsedSchema;
        
        // Symuluj uploadedFile z nazwy pliku (templateId)
        const templateId = this.group.get('templateId')?.value;
        if (templateId) {
          console.log('📄 Restoring file info from templateId:', templateId);
          // Nie możemy odtworzyć prawdziwego File object, ale możemy zachować info że plik był
          // To umożliwi wyświetlenie nazwy i nie zablokuje walidacji
        }
        
        console.log('✅ Schema restored successfully:', Object.keys(parsedSchema).length, 'fields');
      } catch (error) {
        console.error('❌ Failed to parse saved schema:', error);
      }
    }
  }

  onInstructionClick(): void {
    this.instructionRequested.emit();
    // Otwórz PDF z instrukcją w nowej karcie
    window.open('https://www.w3.org/WAI/ER/tests/xhtml/testfiles/resources/pdf/dummy.pdf', '_blank');
  }

  onNoteRemoved(noteId: string): void {
    // Usuń notkę z tablicy i utwórz nową referencję (dla wykrywania zmian)
    this.validationNotes = this.validationNotes.filter(note => note.id !== noteId);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    this.processFile(input.files[0]);
    
    // Reset input value aby umożliwić ponowny wybór tego samego pliku
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const files = event.dataTransfer?.files;
    if (!files || files.length === 0) {
      return;
    }

    const file = files[0];
    
    // Sprawdź rozszerzenie pliku
    if (!isSupportedTemplateFile(file.name)) {
      alert('Nieprawidłowy format pliku. Dopuszczalne formaty: DOCX, XLSX');
      return;
    }

    this.processFile(file);
  }

  private processFile(file: File): void {
    if (!this.group) {
      return;
    }

    // Sprawdź rozszerzenie
    if (!isSupportedTemplateFile(file.name)) {
      alert('Nieprawidłowy format pliku. Dopuszczalne formaty: DOCX, XLSX');
      return;
    }

    // Sprawdź rozmiar (max 10MB)
    const maxSize = 10 * 1024 * 1024; // 10MB
    if (file.size > maxSize) {
      alert('Plik jest za duży. Maksymalny rozmiar: 10MB');
      return;
    }

    // Zapisz plik i nazwę
    this.uploadedFile = file;
    this.group.patchValue({
      templateId: file.name
    });
    this.group.markAsDirty();
    this.group.markAsTouched();

    // Wyślij plik do API
    this.uploadTemplateToApi(file);
  }

  private uploadTemplateToApi(file: File): void {
    this.isUploading.set(true);
    this.isValidating.set(true);
    this.isExtractingPlaceholders.set(true);
    
    // Wykonaj walidację i ekstrakcję równolegle
    forkJoin({
      validation: this.documentService.validateTemplate(file),
      placeholders: this.documentService.extractPlaceholders(file)
    }).subscribe({
      next: ({ validation, placeholders }) => {
        this.isUploading.set(false);
        this.isValidating.set(false);
        this.isExtractingPlaceholders.set(false);
        
        this.processValidationResult(validation);
        this.processPlaceholdersResult(placeholders);
        
        console.log('✅ Walidacja i ekstrakcja zakończone pomyślnie');
        console.log('📋 Validation:', validation);
        console.log('🏷️ Placeholders & Content Controls:', placeholders);
      },
      error: (error) => {
        this.isUploading.set(false);
        this.isValidating.set(false);
        this.isExtractingPlaceholders.set(false);
        console.error('❌ Błąd podczas przetwarzania pliku:', error);
        
        // Pokaż komunikat błędu użytkownikowi
        this.validationNotes = [{
          id: 'error-1',
          type: 'error',
          message: 'Nie udało się przetworzyć dokumentu. Sprawdź połączenie z API.',
          details: [error.message || 'Nieznany błąd']
        }];
      }
    });
  }

  private processValidationResult(validation: DocumentValidationResult): void {
    const notes: ValidationNote[] = [];
    
    // Dodaj błędy z API
    validation.errors.forEach((error, index) => {
      const details: string[] = [];
      
      // Użyj opisu z słownika lub oryginalną wiadomość z API
      const userFriendlyMessage = error.code && ERROR_MESSAGES[error.code] 
        ? ERROR_MESSAGES[error.code] 
        : error.message || 'Nieznany błąd walidacji.';
      
      // Dodaj lokalizację jeśli istnieje
      if (error.location) {
        details.push(error.location);
      }
      
      notes.push({
        id: `error-${index}`,
        type: 'error',
        message: userFriendlyMessage,
        details: details
      });
    });
    
    // Dodaj ostrzeżenia z API
    validation.warnings.forEach((warning, index) => {
      const details: string[] = [];
      
      // Użyj opisu z słownika lub oryginalną wiadomość z API
      const userFriendlyMessage = warning.code && WARNING_MESSAGES[warning.code]
        ? WARNING_MESSAGES[warning.code]
        : warning.message || 'Nieznane ostrzeżenie.';
      
      // Dodaj lokalizację jeśli istnieje
      if (warning.location) {
        details.push(warning.location);
      }
      
      notes.push({
        id: `warning-${index}`,
        type: 'warning',
        message: userFriendlyMessage,
        details: details
      });
    });
    
    // Dodaj komunikat sukcesu jeśli dokument jest poprawny i nie ma błędów
    if (validation.isValid && validation.errors.length === 0) {
      notes.push({
        id: 'success-1',
        type: 'success',
        message: 'Dokument przeszedł walidację pomyślnie.',
        details: []
      });
    }
    
    // Dodaj metadane zawsze jako ostatnie
    if (validation.metadata) {
      const metadataDetails: string[] = [];
      
      metadataDetails.push(`Liczba stron: ${validation.metadata.pageCount}`);
      metadataDetails.push(`Rozmiar: ${Math.round(validation.metadata.fileSizeBytes / 1024)} KB`);
      
      if (validation.metadata.characterCount > 0) {
        metadataDetails.push(`Liczba znaków: ${validation.metadata.characterCount}`);
      }
      
      if (validation.metadata.containsMacros) {
        metadataDetails.push('Dokument zawiera makra');
      }
      
      notes.push({
        id: 'metadata-1',
        type: 'info',
        message: 'Informacje o dokumencie:',
        details: metadataDetails
      });
    }
    
    this.validationNotes = notes;
  }

  private processPlaceholdersResult(result: PlaceholderExtractionResult): void {
    this.extractedPlaceholders = result.placeholders;
    
    // Generuj schemat na podstawie wyekstrahowanych placeholders i content controls
    const schema: Record<string, string> = {};
    
    // Dodaj placeholders jako text
    result.placeholders.forEach(placeholder => {
      // Usuń <% i %> i zapisz jako klucz
      const key = placeholder.replace(/<%|%>/g, '').trim();
      schema[key] = 'text';
    });
    
    // Formanty: typ ze słownika — "boolean" dla wariantu (jak dotąd), "array" dla powtarzanych / list / tabel
    Object.entries(result.contentControls).forEach(([key, value]) => {
      schema[key] = value;
    });

    this.detectedRows.set(buildDetectedElementRows(result.placeholders, result.contentControls, result.contentControlDetails, result.placeholderDetails));
    
    this.generatedSchema = schema;
    this.group.patchValue({
      dataSchemaId: JSON.stringify(schema, null, 2)
    });
    
    // Emit extraction results for worker generation
    this.placeholdersExtracted.emit(result);
    
    // Dodaj notatkę informacyjną o znalezionych polach
    const totalFields = result.placeholdersCount + result.contentControlsCount;
    
    if (totalFields > 0) {
      const details: string[] = [];
      
      if (result.source === 'xlsx') {
        details.push('Szablon arkusza (XLSX): luźne znaczniki z etykietą obok → „Podstaw wartość”; wiersz z samymi znacznikami → wiersz powielany (nagłówek z wiersza wyżej).');
      }
      if (result.placeholdersCount > 0) {
        details.push(`Znaczniki tekstowe (<%...%>): ${result.placeholdersCount}`);
        const samplePlaceholders = result.placeholders.slice(0, 3).join(', ');
        if (samplePlaceholders) {
          details.push(samplePlaceholders);
        }
      }
      
      if (result.contentControlsCount > 0) {
        details.push(result.source === 'xlsx' ? `Wiersze powielane (tabele): ${result.contentControlsCount}` : `Formanty (content controls): ${result.contentControlsCount}`);
        const kinds = summarizeContentControlKinds(result.contentControlDetails ?? []);
        if (kinds.length > 0) {
          details.push(kinds.join(' · '));
        } else {
          const controlKeys = Object.keys(result.contentControls).slice(0, 3).join(', ');
          if (controlKeys) {
            details.push(controlKeys);
          }
        }
      }
      
      this.validationNotes.push({
        id: 'placeholders-info',
        type: 'info',
        message: `Znaleziono ${totalFields} pól w dokumencie.`,
        details: details
      });
    } else {
      this.validationNotes.push({
        id: 'no-placeholders',
        type: 'warning',
        message: 'Nie znaleziono żadnych znaczników ani kontrolek.',
        details: []
      });
    }
  }

  removeFile(): void {
    this.uploadedFile = null;
    this.validationNotes = [];
    this.generatedSchema = null;
    this.detectedRows.set([]);
    this.isShowingApiRequest.set(false);
    this.apiRequestExample = '';
    
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
    
    this.group.patchValue({
      templateId: '',
      dataSchemaId: ''
    });
    this.group.markAsDirty();
    this.group.markAsTouched();
  }

  getUploadedFile(): File | null {
    return this.uploadedFile;
  }

  copySchemaToClipboard(): void {
    const schemaText = this.group.get('dataSchemaId')?.value;
    if (schemaText) {
      navigator.clipboard.writeText(schemaText).then(() => {
        console.log('Schema skopiowana do schowka');
        // TODO: Można dodać toast notification
      }).catch(err => {
        console.error('Błąd podczas kopiowania:', err);
      });
    }
  }

  viewApiRequest(): void {
    if (!this.generatedSchema) {
      return;
    }
    
    if (this.isShowingApiRequest()) {
      // Przełącz z powrotem na schemat danych
      this.isShowingApiRequest.set(false);
    } else {
      // Pokaż API Request
      this.isShowingApiRequest.set(true);
      const apiRequest = this.generateApiRequest(this.generatedSchema);
      this.apiRequestExample = JSON.stringify(apiRequest, null, 2);
    }
  }

  private generateApiRequest(schema: Record<string, any>): any {
    // Generowanie formModel z przykładowymi wartościami dla znalezionych placeholderów
    const formModel: Record<string, any> = {};
    
    Object.keys(schema).forEach(key => {
      // Generuj przykładowe wartości w zależności od typu
      const type = schema[key];
      switch (type) {
        case 'text':
          formModel[key] = `Przykładowa wartość dla ${key}`;
          break;
        case 'number':
          formModel[key] = '123';
          break;
        case 'date':
          formModel[key] = '29.05.2023';
          break;
        case 'boolean':
          formModel[key] = true;
          break;
        case 'array':
          // Fragment powtarzany / lista / tabela: lista elementów — każdy z polami używanymi w tym fragmencie
          formModel[key] = [{ przyklad: 'wartość 1' }, { przyklad: 'wartość 2' }];
          break;
        default:
          formModel[key] = `Wartość ${key}`;
      }
    });

    formModel['documentId'] = null;

    const formSource = this.generalGroup?.get('sourceIdentifier')?.value || 'test';

    return {
      method: 'POST',
      endpoint: 'api/Okapi/CreateForm',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer {token}'
      },
      body: {
        formSource: formSource,
        formModel: formModel,
        data: {
          formSource: null,
          editor: 'AD\\XI81XE',
          roster: 'DefaultRoster',
          krok: '',
          wersjaProcesu: 'INKASO_V1'
        }
      }
    };
  }
}
