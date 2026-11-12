
import { ChangeDetectionStrategy, Component, computed, signal, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { WizardStepHeaderComponent } from '../../components/wizard-step-header/wizard-step-header.component';
import { WizardStepperComponent } from '../../components/wizard-stepper/wizard-stepper.component';
import { WizardFooterComponent } from '../../components/wizard-footer/wizard-footer.component';
import { DocumentDescriptionStepComponent } from '../../steps/document-description-step/document-description-step.component';
import { TemplateAndSchemaStepComponent } from '../../steps/template-and-schema-step/template-and-schema-step.component';
import { ConfigurationStepComponent } from '../../steps/configuration-step/configuration-step.component';
import { SummaryStepComponent } from '../../steps/summary-step/summary-step.component';
import { LetterWizardFacade } from '../../data-access/letter-wizard.facade';
import { LetterWizardData, LetterWizardStepKey } from '../../data-access/letter-wizard.model';
import { ApprovalService, WIZARD_STEP_LABELS, orderedReviewNotes, versionStatusKey } from '../../../admin/services/approval.service';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';
import { Subject, debounceTime, switchMap } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../../core/services/auth.service';
import { SubmissionIssue, getSubmissionIssues, RETENTION_DAYS_MAX, RETENTION_DAYS_MIN } from '../../data-access/letter-wizard-submission';
import { WizardStepperItem } from '../../components/wizard-stepper/wizard-stepper.model';
import { WizardDataService } from '../../services/wizard-data.service';
import { PlaceholderExtractionResult } from '../../services/document.service';
import { ExternalDataService } from '../../services/external-data.service';
import { RecipientDto, BusinessLineDto, StatusDto } from '../../data-access/external-data.model';

@Component({
  selector: 'app-letter-wizard-page',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    ButtonComponent,
    WizardStepHeaderComponent,
    WizardStepperComponent,
    WizardFooterComponent,
    DocumentDescriptionStepComponent,
    TemplateAndSchemaStepComponent,
    ConfigurationStepComponent,
    SummaryStepComponent
],
  templateUrl: './letter-wizard-page.component.html',
  styleUrls: ['./letter-wizard-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LetterWizardPageComponent {
  @ViewChild(TemplateAndSchemaStepComponent) templateAndSchemaStep?: TemplateAndSchemaStepComponent;

  readonly form: FormGroup;
  readonly isSaving = signal(false);
  readonly lastSaved = signal<string | null>(null);
  
  readonly recipients = signal<RecipientDto[]>([]);
  readonly businessLines = signal<BusinessLineDto[]>([]);
  readonly statuses = signal<StatusDto[]>([]);

  readonly stepOrder: LetterWizardStepKey[] = [
    'documentDescription',
    'templateAndSchema',
    'configuration',
    'summary'
  ];

  readonly currentStepIndex = signal(0);

  /**
   * Tryb podglądu (`?mode=view`): kreator pokazuje wersję tylko do odczytu — formularz wyłączony,
   * bez zapisu, uploadu i edycji workerów; stepper pozwala chodzić po wszystkich krokach.
   * Służy do porównania, co zrobiono w innych (także aktywnych) wersjach projektu.
   */
  readonly readOnly = signal(false);

  /**
   * Tryb przeglądu administratora (`?mode=review`, z „Zgłoszenia do publikacji”): jak podgląd, ale z polem
   * uwag przy każdym kroku (zapis do API z opóźnieniem) i decyzją — zaakceptuj / odeślij do poprawy — na końcu.
   */
  readonly reviewMode = signal(false);
  /** Skąd otwarto kreator (`?from=`): „Zamknij” wraca do modułu administracyjnego albo do szczegółów projektu. */
  private origin: string | null = null;

  /** Uwagi recenzenta do kroków: klucz kroku → treść (edytowane w przeglądzie, tylko do odczytu dla projektanta). */
  readonly reviewNotes = signal<Record<string, string>>({});
  readonly notesSaveState = signal<'idle' | 'dirty' | 'saving' | 'saved' | 'error'>('idle');
  private readonly notesChanged$ = new Subject<void>();
  readonly currentNote = computed(() => this.reviewNotes()[this.currentStepKey()] ?? '');
  readonly notesCount = computed(() => Object.values(this.reviewNotes()).filter((t) => t.trim()).length);
  readonly orderedNotes = computed(() => orderedReviewNotes(this.reviewNotes()));
  /** Projektant: wersja odesłana do poprawy (albo z uwagami) — pokaż uwagi recenzenta przy krokach. */
  readonly showReviewerFeedback = computed(() => !this.reviewMode() && (versionStatusKey(this.facade.versionStatus()) === 'changes' || this.notesCount() > 0 || !!this.facade.reviewComment()));
  readonly reviewerComment = computed(() => this.facade.reviewComment());
  readonly reviewerName = computed(() => this.facade.reviewedBy());
  readonly stepLabel = computed(() => WIZARD_STEP_LABELS[this.currentStepKey()]);
  readonly versionTag = computed(() => this.facade.versionTag());

  /** Dialog decyzji w przeglądzie. */
  readonly decision = signal<'approve' | 'reject' | null>(null);
  decisionComment = '';
  readonly isDeciding = signal(false);
  readonly decisionError = signal<string | null>(null);

  /**
   * Wskaźnik ładowania przy zmianie kroku: półprzezroczysta nakładka ze spinnerem od kliknięcia do chwili,
   * gdy krok zgłosi `(ready)` (słowniki, workery) — minimum 250 ms, żeby nie migał; awaryjnie znika po 4 s.
   */
  readonly stepLoading = signal(false);
  private stepLoadingSince = 0;
  private stepLoadingTimer: ReturnType<typeof setTimeout> | undefined;

  /**
   * Braki blokujące „Wyślij do weryfikacji" (dane biznesowe, retencja). W trakcie pracy nad projektem
   * te pola są opcjonalne; komplet jest wymagany dopiero przy finalizacji. Odświeżane przy każdej zmianie kroku
   * i przy próbie wysłania — źródło reguł: `letter-wizard-submission.ts`.
   */
  readonly submissionIssues = signal<SubmissionIssue[]>([]);

  readonly currentStepKey = computed<LetterWizardStepKey>(
    () => this.stepOrder[this.currentStepIndex()]
  );

  readonly stepperItems = computed<WizardStepperItem[]>(() => {
    const activeIndex = this.currentStepIndex();
    const completedSteps = this.facade.completedSteps();

    const labelMap: Record<LetterWizardStepKey, string> = {
      documentDescription: 'Opis dokumentu',
      templateAndSchema: 'Szablon i schemat danych',
      configuration: 'Konfiguracja',
      summary: 'Podsumowanie'
    };

    return this.stepOrder.map((key, index) => {
      let status: 'todo' | 'in-progress' | 'accepted' = 'todo';

      if (this.readOnly() && index !== activeIndex) {
        // Podgląd: każdy krok dostępny do przejrzenia
        status = 'accepted';
      } else if (index === activeIndex) {
        // Aktualny krok - zawsze w trakcie (pomarańczowy)
        status = 'in-progress';
      } else if (completedSteps.includes(key)) {
        // Krok został ukończony - zaakceptowany (zielony)
        status = 'accepted';
      } else if (index < activeIndex) {
        // Krok był przeglądany ale niekoniecznie ukończony - też zaakceptowany
        status = 'accepted';
      }

      return {
        key,
        index: index + 1,
        label: labelMap[key],
        status
      };
    });
  });

  readonly stepTitle = computed(() => {
    if (!this.facade.isEditMode()) {
      return 'Nowy dokument';
    }
    const tag = this.facade.versionTag();
    if (this.reviewMode()) {
      return tag ? `Przegląd dokumentu — wersja ${tag} (do akceptacji)` : 'Przegląd dokumentu';
    }
    if (this.readOnly()) {
      const state = this.facade.isActiveVersion() ? 'aktywna' : 'robocza';
      return tag ? `Podgląd dokumentu — wersja ${tag} (${state})` : 'Podgląd dokumentu';
    }
    const suffix = this.facade.isActiveVersion() ? ' (aktywna!)' : '';
    return tag ? `Edycja dokumentu — wersja ${tag}${suffix}` : 'Edycja dokumentu';
  });

  readonly stepDescription = computed(() => {
    const map: Record<LetterWizardStepKey, string> = {
      documentDescription: 'Wskaż istotne dane o dokumencie.',
      templateAndSchema: 'Wybierz szablon i schemat danych.',
      configuration: 'Skonfiguruj sposób działania dokumentu.',
      summary: 'Sprawdź i zatwierdź konfigurację dokumentu.'
    };
    return map[this.currentStepKey()];
  });

  projectId: string | null = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly facade: LetterWizardFacade,
    private readonly wizardDataService: WizardDataService,
    private readonly externalDataService: ExternalDataService,
    private readonly approvalService: ApprovalService,
    private readonly authService: AuthService
  ) {
    this.form = this.fb.group({
      general: this.fb.group({
        documentName: ['', Validators.required],
        sourceIdentifier: [''],
        applicationName: ['', Validators.required],
        // Odbiorca i retencja: opcjonalne w trakcie pracy, wymagane przed „Wyślij do weryfikacji" (letter-wizard-submission.ts)
        recipient: [null],
        retentionDays: [90, [Validators.min(RETENTION_DAYS_MIN), Validators.max(RETENTION_DAYS_MAX)]],
        environments: this.fb.control<string[]>([])
      }),
      // Dane biznesowe: opcjonalne w trakcie pracy nad projektem pisma, wymagane dopiero przed
      // „Wyślij do weryfikacji" (patrz getSubmissionIssues). Backend też ich nie wymaga przy tworzeniu wersji.
      business: this.fb.group({
        processName: [''],
        businessLine: [null],
        tenant: [null], // Optional - can create personal projects or team projects
        processOwner: [''],
        processVersion: [''],
        processStep: [''],
        status: ['']
      }),
      templateAndSchema: this.fb.group({
        templateId: [''],
        dataSchemaId: [''],
        defaultLanguage: [''],
        outputFormat: ['pdf']
      }),
      configuration: this.fb.group({
        autoSendEnabled: [false],
        requiresApproval: [false],
        approvalRole: [''],
        visibilityScope: ['internal']
      })
    });

    this.projectId = this.route.snapshot.paramMap.get('projectId');
    const versionId = this.route.snapshot.queryParamMap.get('versionId');
    const mode = this.route.snapshot.queryParamMap.get('mode');
    this.origin = this.route.snapshot.queryParamMap.get('from');

    // Uwagi recenzenta: zapis do API 600 ms po ostatniej zmianie (tylko w przeglądzie)
    this.notesChanged$.pipe(
      debounceTime(600),
      switchMap(() => {
        const versionId = this.facade.state()?.meta.id;
        this.notesSaveState.set('saving');
        return this.approvalService.saveNotes(versionId!, this.reviewNotes());
      }),
      takeUntilDestroyed()
    ).subscribe({
      next: () => this.notesSaveState.set('saved'),
      error: (error) => {
        console.error('Błąd zapisu uwag recenzenta:', error);
        this.notesSaveState.set('error');
      }
    });
    
    if (this.projectId) {
      if (versionId && (mode === 'edit' || mode === 'view' || mode === 'review')) {
        this.readOnly.set(mode === 'view' || mode === 'review');
        this.reviewMode.set(mode === 'review');
        // Tryb edycji - ładuj dane z API
        console.log('🔄 Loading existing project for edit:', this.projectId, versionId);
        
        this.facade.loadProjectData(this.projectId, versionId).subscribe({
          next: (success) => {
            if (success) {
              console.log('✅ Project data loaded successfully');
              
              // Populate form with loaded data
              const wizardData = this.facade.state()?.data;
              if (wizardData) {
                console.log('📋 Filling form with loaded data:', wizardData);
                this.form.patchValue(wizardData);
                
                // Force change detection
                this.form.updateValueAndValidity();
              }

              this.reviewNotes.set({ ...(this.facade.reviewNotes() ?? {}) });

              if (this.readOnly()) {
                // Podgląd / przegląd: formularz tylko do odczytu, start od pierwszego kroku
                this.form.disable({ emitEvent: false });
                this.currentStepIndex.set(0);
                this.refreshSubmissionIssues();
                return;
              }

              // Set current step from facade
              const currentStep = this.facade.currentStep();
              if (currentStep) {
                const stepIndex = this.stepOrder.indexOf(currentStep);
                this.currentStepIndex.set(stepIndex >= 0 ? stepIndex : 0);
                this.refreshSubmissionIssues();
                console.log('🎯 Current step set to:', currentStep, 'index:', stepIndex);
              }
            } else {
              console.error('❌ Failed to load project data');
              alert('Nie udało się załadować danych projektu');
              this.router.navigate(['/projects']);
            }
          },
          error: (error) => {
            console.error('❌ Error loading project:', error);
            alert('Wystąpił błąd podczas ładowania projektu');
            this.router.navigate(['/projects']);
          }
        });
      } else {
        // Tryb tworzenia nowego
        this.facade.initNew(this.projectId);
      }
    }
    
    this.loadDictionaries();
  }
  
  private loadDictionaries(): void {
    this.externalDataService.getRecipients().subscribe({
      next: (data) => this.recipients.set(data),
      error: (err) => console.error('❌ Error loading recipients:', err)
    });
    
    this.externalDataService.getBusinessLines().subscribe({
      next: (data: BusinessLineDto[]) => this.businessLines.set(data),
      error: (err: any) => console.error('❌ Error loading business lines:', err)
    });
    
    this.externalDataService.getStatuses().subscribe({
      next: (data: StatusDto[]) => this.statuses.set(data),
      error: (err: any) => console.error('❌ Error loading statuses:', err)
    });
  }

  onPlaceholdersExtracted(result: PlaceholderExtractionResult): void {
    console.log('📋 Placeholders extracted, storing for worker generation');
    this.facade.storePlaceholderExtractionResult(result);
  }

  onStepperClick(item: WizardStepperItem): void {
    // Pozwól na kliknięcie tylko zaakceptowanych kroków lub aktualnego
    if (item.status === 'todo') {
      return;
    }

    const targetIndex = item.index - 1;
    if (targetIndex < 0 || targetIndex >= this.stepOrder.length) {
      return;
    }

    if (this.readOnly()) {
      this.setStep(targetIndex);
      return;
    }

    // Zapisz aktualny stan przed zmianą kroku
    const value = this.form.getRawValue() as LetterWizardData;
    this.facade.updateData(value);
    
    // Przejdź do wybranego kroku
    this.setStep(targetIndex);
  }

  onSave(): void {
    if (this.readOnly()) {
      return;
    }
    const value = this.form.getRawValue() as LetterWizardData;
    this.facade.updateData(value);
    
    this.isSaving.set(true);
    console.log('💾 Zapisywanie bieżącego stanu kreatora...');

    // Zapisz dane w zależności od aktualnego kroku
    const currentStep = this.currentStepKey();
    
    if (currentStep === 'documentDescription') {
      // Krok 1 - zapis podstawowych danych projektu
      this.facade.saveFirstStepData().subscribe({
        next: (result) => {
          this.isSaving.set(false);
          if (result) {
            const now = new Date().toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' });
            this.lastSaved.set(now);
            console.log('✅ Stan kreatora zapisany pomyślnie');
          } else {
            console.error('❌ Błąd zapisu');
            alert('Wystąpił błąd podczas zapisywania');
          }
        },
        error: (error) => {
          this.isSaving.set(false);
          console.error('❌ Błąd podczas zapisywania:', error);
          alert('Wystąpił błąd podczas zapisywania');
        }
      });
    } else if (currentStep === 'templateAndSchema') {
      // Krok 2 - zapis szablonu
      const uploadedFile = this.templateAndSchemaStep?.getUploadedFile();
      
      this.facade.saveSecondStepData(uploadedFile || null).subscribe({
        next: (result) => {
          this.isSaving.set(false);
          if (result) {
            const now = new Date().toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' });
            this.lastSaved.set(now);
            console.log('✅ Stan kreatora zapisany pomyślnie');
          } else {
            console.error('❌ Błąd zapisu');
            alert('Wystąpił błąd podczas zapisywania');
          }
        },
        error: (error) => {
          this.isSaving.set(false);
          console.error('❌ Błąd podczas zapisywania:', error);
          alert('Wystąpił błąd podczas zapisywania');
        }
      });
    } else {
      // Pozostałe kroki - po prostu aktualizuj stepData
      const projectId = this.facade.state()?.meta.projectId;
      const versionId = this.facade.state()?.meta.id;
      
      if (projectId && versionId) {
        this.wizardDataService.updateProjectVersionStepData(projectId, versionId, value).subscribe({
          next: () => {
            this.isSaving.set(false);
            const now = new Date().toLocaleTimeString('pl-PL', { hour: '2-digit', minute: '2-digit' });
            this.lastSaved.set(now);
            console.log('✅ Stan kreatora zapisany pomyślnie');
          },
          error: (error) => {
            this.isSaving.set(false);
            console.error('❌ Błąd podczas zapisywania:', error);
            alert('Wystąpił błąd podczas zapisywania');
          }
        });
      } else {
        this.isSaving.set(false);
        console.error('❌ Brak ID projektu lub wersji');
        alert('Nie można zapisać - brak ID projektu');
      }
    }
  }

  onNext(): void {
    if (this.readOnly()) {
      // Podgląd: tylko nawigacja, ostatni krok zamyka podgląd
      const idx = this.currentStepIndex();
      if (idx < this.stepOrder.length - 1) {
        this.setStep(idx + 1);
      } else {
        this.onCancel();
      }
      return;
    }

    // Walidacja dla kroku "Opis dokumentu"
    if (this.currentStepKey() === 'documentDescription') {
      // Oznacz wszystkie pola jako touched aby pokazać błędy
      this.generalGroup.markAllAsTouched();
      this.businessGroup.markAllAsTouched();
      
      // Sprawdź czy formularze są poprawne
      if (this.generalGroup.invalid || this.businessGroup.invalid) {
        console.warn('⚠️ Formularz zawiera błędy walidacji');
        return; // Zatrzymaj przejście do następnego kroku
      }

      // Po walidacji pierwszego kroku - zapisz dane do backendu
      const value = this.form.getRawValue() as LetterWizardData;
      this.facade.updateData(value);

      console.log('💾 Zapisuję dane pierwszego kroku...');
      this.facade.saveFirstStepData().subscribe({
        next: (result) => {
          if (result) {
            console.log('✅ Dane pierwszego kroku zapisane pomyślnie');
            console.log('   Project ID:', result.project.id);
            console.log('   Version ID:', result.version.id);
            
            // Oznacz krok jako ukończony i przejdź dalej
            const idx = this.currentStepIndex();
            const key = this.currentStepKey();
            this.facade.markStepCompleted(key);
            this.setStep(idx + 1);
          } else {
            console.error('❌ Błąd zapisu - brak wyniku');
            alert('Wystąpił błąd podczas zapisywania danych. Sprawdź konsolę.');
          }
        },
        error: (error) => {
          console.error('❌ Błąd podczas zapisywania danych pierwszego kroku:', error);
          alert('Wystąpił błąd podczas zapisywania danych. Sprawdź konsolę.');
        }
      });
      return;
    }

    // Walidacja dla kroku "Szablon i schemat danych"
    if (this.currentStepKey() === 'templateAndSchema') {
      // Oznacz formularz jako touched
      this.templateAndSchemaGroup.markAllAsTouched();

      // Sprawdź czy plik został wybrany
      const templateId = this.templateAndSchemaGroup.get('templateId')?.value;
      if (!templateId) {
        console.warn('⚠️ Nie wybrano pliku szablonu');
        alert('Proszę wybrać plik szablonu (.docx lub .xlsx)');
        return;
      }

      // Sprawdź czy schemat został wygenerowany
      const dataSchemaId = this.templateAndSchemaGroup.get('dataSchemaId')?.value;
      if (!dataSchemaId) {
        console.warn('⚠️ Schemat danych nie został wygenerowany');
        alert('Schemat danych nie został wygenerowany. Spróbuj ponownie wczytać plik.');
        return;
      }

      // Pobierz plik z komponentu kroku
      const uploadedFile = this.templateAndSchemaStep?.getUploadedFile();
      
      // Jeśli nie ma pliku ale mamy templateId (tryb edycji), to OK - plik już jest w systemie
      if (!uploadedFile && !templateId) {
        console.warn('⚠️ Nie znaleziono wczytanego pliku');
        alert('Nie znaleziono wczytanego pliku. Spróbuj ponownie.');
        return;
      }

      // Zaktualizuj dane w facade
      const value = this.form.getRawValue() as LetterWizardData;
      this.facade.updateData(value);

      console.log('💾 Zapisuję dane drugiego kroku...');
      if (uploadedFile) {
        console.log('   Plik:', uploadedFile.name);
      } else {
        console.log('   Plik już zapisany (templateId):', templateId);
      }
      console.log('   Schemat:', dataSchemaId.substring(0, 100) + '...');

      this.facade.saveSecondStepData(uploadedFile || null).subscribe({
        next: (result) => {
          if (result) {
            console.log('✅ Dane drugiego kroku zapisane pomyślnie');
            if (result.fileUpload) {
              console.log('   File ID:', result.fileUpload.fileId);
            }
            console.log('   Step data updated');
            
            // Oznacz krok jako ukończony i przejdź dalej
            const idx = this.currentStepIndex();
            const key = this.currentStepKey();
            this.facade.markStepCompleted(key);
            this.setStep(idx + 1);
          } else {
            console.error('❌ Błąd zapisu - brak wyniku');
            alert('Wystąpił błąd podczas zapisywania danych. Sprawdź konsolę.');
          }
        },
        error: (error) => {
          console.error('❌ Błąd podczas zapisywania danych drugiego kroku:', error);
          alert('Wystąpił błąd podczas zapisywania danych. Sprawdź konsolę.');
        }
      });
      return;
    }

    // Dla pozostałych kroków - standardowe przejście
    const value = this.form.getRawValue() as LetterWizardData;
    this.facade.updateData(value);

    const idx = this.currentStepIndex();
    const key = this.currentStepKey();

    // Finalizacja („Wyślij do weryfikacji"): dopiero tu dane biznesowe i retencja są wymagane.
    if (idx === this.stepOrder.length - 1) {
      const issues = getSubmissionIssues(value);
      this.submissionIssues.set(issues);
      if (issues.length > 0) {
        console.warn('⚠️ Nie można wysłać do weryfikacji — brakuje danych:', issues.map(i => i.label));
        queueMicrotask(() => document.querySelector('.submission-issues')?.scrollIntoView({ behavior: 'smooth', block: 'start' }));
        return;
      }
    }

    this.facade.markStepCompleted(key);

    if (idx < this.stepOrder.length - 1) {
      this.setStep(idx + 1);
    } else {
      // „Wyślij do weryfikacji” (BR-017): zapisz bieżący stan i zgłoś wersję do akceptacji administratora,
      // potem wróć do szczegółów projektu — tam widać status „Do akceptacji” i później decyzję.
      const projectId = this.facade.getCreatedProjectId() || this.projectId;
      const versionId = this.facade.state()?.meta.id;
      if (!projectId || !versionId) {
        console.warn('Brak identyfikatora projektu/wersji — nie można zgłosić do weryfikacji');
        this.router.navigate(['/projects']);
        return;
      }
      this.isSaving.set(true);
      this.wizardDataService.updateProjectVersionStepData(projectId, versionId, value).subscribe({
        next: () => this.approvalService.submit(versionId, this.authService.getCurrentUser()?.name ?? null).subscribe({
          next: () => {
            this.isSaving.set(false);
            this.router.navigate(['/projects', projectId]);
          },
          error: (error) => {
            this.isSaving.set(false);
            console.error('Błąd zgłoszenia do weryfikacji:', error);
            alert(error?.error?.message ?? 'Nie udało się zgłosić wersji do weryfikacji');
          }
        }),
        error: (error) => {
          this.isSaving.set(false);
          console.error('Błąd zapisu przed zgłoszeniem:', error);
          alert('Nie udało się zapisać wersji przed zgłoszeniem');
        }
      });
    }
  }

  onBack(): void {
    const idx = this.currentStepIndex();
    if (idx === 0) {
      return;
    }
    this.setStep(idx - 1);
  }

  /** „Zamknij”/„Anuluj”: powrót tam, skąd przyszliśmy — moduł administracyjny albo szczegóły projektu. */
  onCancel(): void {
    if (this.origin === 'admin-approvals') {
      this.router.navigate(['/admin/approvals']);
      return;
    }
    if (this.projectId) {
      this.router.navigate(['/projects', this.projectId], this.origin ? { queryParams: { from: this.origin } } : {});
    } else {
      this.router.navigate(['/projects']);
    }
  }

  // --- przegląd administratora: uwagi do kroków + decyzja ----------------------------------------

  onNoteChange(text: string): void {
    this.reviewNotes.update((notes) => ({ ...notes, [this.currentStepKey()]: text }));
    this.notesSaveState.set('dirty');
    this.notesChanged$.next();
  }

  openDecision(kind: 'approve' | 'reject'): void {
    this.decisionComment = '';
    this.decisionError.set(null);
    this.decision.set(kind);
  }

  closeDecision(): void {
    this.decision.set(null);
  }

  /** Odesłanie wymaga komentarza ogólnego albo choć jednej uwagi do kroku. */
  canConfirmDecision(): boolean {
    if (this.decision() === 'reject') {
      return !!this.decisionComment.trim() || this.notesCount() > 0;
    }
    return true;
  }

  confirmDecision(): void {
    const kind = this.decision();
    const versionId = this.facade.state()?.meta.id;
    if (!kind || !versionId || !this.canConfirmDecision()) {
      return;
    }
    const reviewer = this.authService.getCurrentUser()?.name ?? 'admin';
    const comment = this.decisionComment.trim() || null;
    this.isDeciding.set(true);
    this.decisionError.set(null);
    // Najpierw dopilnuj zapisu uwag (mogły być zmienione przed chwilą), potem decyzja
    this.approvalService.saveNotes(versionId, this.reviewNotes()).pipe(
      switchMap(() => kind === 'approve'
        ? this.approvalService.approve(versionId, reviewer, comment)
        : this.approvalService.reject(versionId, reviewer, comment))
    ).subscribe({
      next: () => {
        this.isDeciding.set(false);
        this.decision.set(null);
        this.router.navigate(['/admin/approvals']);
      },
      error: (error) => {
        console.error('Błąd decyzji:', error);
        this.isDeciding.set(false);
        this.decisionError.set(error?.error?.message ?? 'Nie udało się zapisać decyzji.');
      }
    });
  }

  /** Przejście do kroku z panelu braków w podsumowaniu (zapisuje bieżący stan formularza do fasady). */
  onGoToStep(step: LetterWizardStepKey): void {
    const targetIndex = this.stepOrder.indexOf(step);
    if (targetIndex < 0) {
      return;
    }
    if (!this.readOnly()) {
      const value = this.form.getRawValue() as LetterWizardData;
      this.facade.updateData(value);
    }
    this.setStep(targetIndex);
  }

  /** Jedno miejsce zmiany kroku: indeks + fasada + odświeżenie braków do wysyłki. */
  private setStep(index: number): void {
    if (index !== this.currentStepIndex()) {
      this.beginStepLoading();
    }
    this.currentStepIndex.set(index);
    this.facade.moveToStep(this.stepOrder[index]);
    this.refreshSubmissionIssues();
  }

  /** Krok zgłosił gotowość (dane wczytane) — chowamy nakładkę, ale nie wcześniej niż po 250 ms. */
  onStepReady(): void {
    if (!this.stepLoading()) {
      return;
    }
    const remaining = Math.max(0, 250 - (Date.now() - this.stepLoadingSince));
    clearTimeout(this.stepLoadingTimer);
    this.stepLoadingTimer = setTimeout(() => this.stepLoading.set(false), remaining);
  }

  private beginStepLoading(): void {
    this.stepLoading.set(true);
    this.stepLoadingSince = Date.now();
    clearTimeout(this.stepLoadingTimer);
    this.stepLoadingTimer = setTimeout(() => this.stepLoading.set(false), 4000);
  }

  private refreshSubmissionIssues(): void {
    this.submissionIssues.set(getSubmissionIssues(this.form.getRawValue() as LetterWizardData));
  }

  get generalGroup(): FormGroup {
    return this.form.get('general') as FormGroup;
  }

  get businessGroup(): FormGroup {
    return this.form.get('business') as FormGroup;
  }

  get templateAndSchemaGroup(): FormGroup {
    return this.form.get('templateAndSchema') as FormGroup;
  }

  get configurationGroup(): FormGroup {
    return this.form.get('configuration') as FormGroup;
  }
}
