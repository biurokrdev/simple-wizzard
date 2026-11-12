import { Injectable, computed, signal, inject } from '@angular/core';
import { Observable, switchMap, tap, catchError, of, forkJoin, throwError } from 'rxjs';
import {
  LetterWizardData,
  LetterWizardMeta,
  LetterWizardState,
  LetterWizardStepKey
} from './letter-wizard.model';
import { WizardDataService, ProjectResponse, ProjectVersionResponse, FileUploadResponse } from '../services/wizard-data.service';
import { WorkerService } from '../services/worker.service';
import { PlaceholderExtractionResult } from '../services/document.service';

@Injectable({
  providedIn: 'root'
})
export class LetterWizardFacade {
  private readonly wizardDataService = inject(WizardDataService);
  private readonly workerService = inject(WorkerService);
  private readonly initialData: LetterWizardData = {
    general: {
      documentName: '',
      sourceIdentifier: '',
      applicationName: '',
      recipient: null,
      retentionDays: 90,
      environments: []
    },
    business: {
      processName: '',
      businessLine: null,
      tenant: '',
      processOwner: '',
      processVersion: '',
      processStep: '',
      status: ''
    },
    templateAndSchema: {
      templateId: '',
      dataSchemaId: '',
      defaultLanguage: '',
      outputFormat: 'pdf'
    },
    configuration: {
      autoSendEnabled: false,
      requiresApproval: false,
      approvalRole: '',
      visibilityScope: 'internal'
    }
  };

  private readonly stateSignal = signal<LetterWizardState | null>(null);

  readonly state = computed(() => this.stateSignal());
  readonly currentStep = computed<LetterWizardStepKey | null>(
    () => this.stateSignal()?.meta.currentStep ?? null
  );
  readonly isEditMode = computed<boolean>(
    () => !!this.stateSignal()?.meta.id
  );
  readonly completedSteps = computed<LetterWizardStepKey[]>(
    () => this.stateSignal()?.meta.completedSteps ?? []
  );
  readonly versionTag = computed<string | null>(() => this.stateSignal()?.meta.versionTag ?? null);
  readonly isActiveVersion = computed<boolean>(() => this.stateSignal()?.meta.isActiveVersion ?? false);
  readonly versionStatus = computed<string | number | null>(() => this.stateSignal()?.meta.status ?? null);
  readonly reviewNotes = computed<Record<string, string> | null>(() => this.stateSignal()?.meta.reviewNotes ?? null);
  readonly reviewComment = computed<string | null>(() => this.stateSignal()?.meta.reviewComment ?? null);
  readonly reviewedBy = computed<string | null>(() => this.stateSignal()?.meta.reviewedBy ?? null);

  // Store the created project and version IDs
  private createdProjectId: string | null = null;
  private createdVersionId: string | null = null;
  
  // Store placeholder extraction results for worker generation
  private lastPlaceholderExtractionResult: PlaceholderExtractionResult | null = null;

  initNew(projectId: string): void {
    const meta: LetterWizardMeta = {
      projectId,
      currentStep: 'documentDescription',
      completedSteps: []
    };

    this.stateSignal.set({ data: this.initialData, meta });
  }

  initEdit(projectId: string, documentId: string, existingData: LetterWizardData, currentStep?: LetterWizardStepKey, completedSteps?: LetterWizardStepKey[]): void {
    const meta: LetterWizardMeta = {
      id: documentId,
      projectId,
      currentStep: currentStep ?? 'documentDescription',
      completedSteps: completedSteps ?? []
    };

    this.stateSignal.set({ data: existingData, meta });
  }

  updateData(partialData: Partial<LetterWizardData>): void {
    const current = this.stateSignal();
    if (!current) {
      return;
    }

    this.stateSignal.set({
      ...current,
      data: {
        ...current.data,
        ...partialData
      },
      meta: {
        ...current.meta,
        lastUpdatedAt: new Date().toISOString()
      }
    });
  }

  /**
   * Store placeholder extraction results for later worker generation
   */
  storePlaceholderExtractionResult(result: PlaceholderExtractionResult): void {
    console.log('💾 Storing placeholder extraction result for worker generation');
    this.lastPlaceholderExtractionResult = result;
  }

  /**
   * Get stored placeholder extraction results
   */
  getPlaceholderExtractionResult(): PlaceholderExtractionResult | null {
    return this.lastPlaceholderExtractionResult;
  }

  moveToStep(step: LetterWizardStepKey): void {
    const current = this.stateSignal();
    if (!current) {
      return;
    }

    this.stateSignal.set({
      ...current,
      meta: {
        ...current.meta,
        currentStep: step
      }
    });
  }

  markStepCompleted(step: LetterWizardStepKey): void {
    const current = this.stateSignal();
    if (!current) {
      return;
    }

    const completed = new Set(current.meta.completedSteps);
    completed.add(step);

    this.stateSignal.set({
      ...current,
      meta: {
        ...current.meta,
        completedSteps: Array.from(completed),
        lastUpdatedAt: new Date().toISOString()
      }
    });
  }

  /**
   * Saves the first step data (creates project and version OR updates existing)
   * Returns Observable that emits when save is complete
   */
  saveFirstStepData(): Observable<{ project: ProjectResponse; version: ProjectVersionResponse } | null> {
    const current = this.stateSignal();
    if (!current) {
      console.error('No wizard state to save');
      return of(null);
    }

    const projectId = current.meta.projectId;
    const data = current.data;
    const isEditMode = !!current.meta.id; // If we have version ID, we're editing

    console.log('💾 Saving first step data for project:', projectId);
    console.log('   Edit mode:', isEditMode);

    // If we're in edit mode, just update the existing version
    if (isEditMode && this.createdProjectId && this.createdVersionId) {
      console.log('🔄 Updating existing version:', this.createdVersionId);
      
      return this.wizardDataService.updateProjectVersionStepData(
        this.createdProjectId,
        this.createdVersionId,
        data
      ).pipe(
        switchMap(() => forkJoin({
          project: this.wizardDataService.getProject(this.createdProjectId!),
          version: this.wizardDataService.getProjectVersion(this.createdProjectId!, this.createdVersionId!)
        })),
        tap(result => {
          console.log('✅ Version updated:', result.version);
        }),
        catchError(error => {
          console.error('❌ Error updating version:', error);
          return of(null);
        })
      );
    }

    // Otherwise, create new project and version
    console.log('✨ Creating new project and version');
    console.log('📦 Data being sent:', JSON.stringify(data, null, 2));
    return this.wizardDataService.createProject(data, projectId).pipe(
      tap(project => {
        console.log('✅ Project created:', project);
        this.createdProjectId = project.id;
        
        // Update state with project ID
        const current = this.stateSignal();
        if (current) {
          this.stateSignal.set({
            ...current,
            meta: {
              ...current.meta,
              projectId: project.id
            }
          });
        }
      }),
      switchMap(project => 
        this.wizardDataService.createProjectVersion(project.id, data).pipe(
          tap(version => {
            console.log('✅ Project version created:', version);
            this.createdVersionId = version.id;
            
            // Update state with version ID
            const current = this.stateSignal();
            if (current) {
              this.stateSignal.set({
                ...current,
                meta: {
                  ...current.meta,
                  id: version.id
                }
              });
            }
          }),
          switchMap(version => of({ project, version }))
        )
      ),
      catchError(error => {
        console.error('❌ Error saving first step data:', error);
        console.error('❌ Error details:', {
          status: error.status,
          statusText: error.statusText,
          message: error.message,
          error: error.error
        });
        return throwError(() => error);
      })
    );
  }

  /**
   * Saves the second step data (uploads template file and updates step data)
   * Also generates workers from placeholders and content controls
   * Returns Observable that emits when save is complete
   */
  saveSecondStepData(templateFile: File | null): Observable<{ fileUpload: FileUploadResponse | null; stepDataUpdate: any; workers?: any[] } | null> {
    const current = this.stateSignal();
    if (!current) {
      console.error('No wizard state to save');
      return of(null);
    }

    if (!this.createdProjectId || !this.createdVersionId) {
      console.error('❌ No project or version ID available.');
      return of(null);
    }

    // Add detected elements to templateAndSchema before saving
    const data = { ...current.data };
    if (this.lastPlaceholderExtractionResult) {
      data.templateAndSchema = {
        ...data.templateAndSchema,
        detectedElements: {
          placeholders: this.lastPlaceholderExtractionResult.placeholders || [],
          contentControls: this.lastPlaceholderExtractionResult.contentControls || {},
          contentControlDetails: this.lastPlaceholderExtractionResult.contentControlDetails ?? [],
          placeholderDetails: this.lastPlaceholderExtractionResult.placeholderDetails ?? [],
          source: this.lastPlaceholderExtractionResult.source ?? 'docx'
        }
      };
      console.log('📋 Adding detected elements to step data:', data.templateAndSchema.detectedElements);
    }

    console.log('💾 Saving second step data...');
    console.log('   Project ID:', this.createdProjectId);
    console.log('   Version ID:', this.createdVersionId);
    console.log('   Template file:', templateFile?.name);

    // If there's a file, upload it first, then update step data, then generate workers
    // If no file, just update step data and generate workers
    if (templateFile) {
      return this.wizardDataService.uploadTemplateFile(this.createdVersionId, templateFile).pipe(
        tap(fileResponse => console.log('✅ Template file uploaded:', fileResponse)),
        switchMap(fileResponse =>
          this.wizardDataService.updateProjectVersionStepData(this.createdProjectId!, this.createdVersionId!, data).pipe(
            tap(stepDataResponse => console.log('✅ Step data updated:', stepDataResponse)),
            switchMap(stepDataResponse => {
              // Generate workers if we have extraction results
              if (this.lastPlaceholderExtractionResult) {
                console.log('🔧 Generating workers from extracted placeholders and controls...');
                return this.workerService.generateWorkers({
                  projectVersionId: this.createdVersionId!,
                  placeholders: this.lastPlaceholderExtractionResult.placeholders,
                  contentControls: this.lastPlaceholderExtractionResult.contentControls,
                  contentControlDetails: this.lastPlaceholderExtractionResult.contentControlDetails ?? [],
                  placeholderDetails: this.lastPlaceholderExtractionResult.placeholderDetails ?? []
                }).pipe(
                  tap(workers => console.log('✅ Workers generated:', workers.length)),
                  switchMap(workers => of({ fileUpload: fileResponse, stepDataUpdate: stepDataResponse, workers })),
                  catchError(error => {
                    console.error('⚠️ Error generating workers (continuing anyway):', error);
                    return of({ fileUpload: fileResponse, stepDataUpdate: stepDataResponse });
                  })
                );
              } else {
                return of({ fileUpload: fileResponse, stepDataUpdate: stepDataResponse });
              }
            })
          )
        ),
        catchError(error => {
          console.error('❌ Error saving second step data:', error);
          return of(null);
        })
      );
    } else {
      // No file to upload, just update step data and generate workers
      return this.wizardDataService.updateProjectVersionStepData(this.createdProjectId, this.createdVersionId, data).pipe(
        tap(stepDataResponse => console.log('✅ Step data updated:', stepDataResponse)),
        switchMap(stepDataResponse => {
          // Generate workers if we have extraction results
          if (this.lastPlaceholderExtractionResult) {
            console.log('🔧 Generating workers from extracted placeholders and controls...');
            return this.workerService.generateWorkers({
              projectVersionId: this.createdVersionId!,
              placeholders: this.lastPlaceholderExtractionResult.placeholders,
              contentControls: this.lastPlaceholderExtractionResult.contentControls,
              contentControlDetails: this.lastPlaceholderExtractionResult.contentControlDetails ?? [],
              placeholderDetails: this.lastPlaceholderExtractionResult.placeholderDetails ?? []
            }).pipe(
              tap(workers => console.log('✅ Workers generated:', workers.length)),
              switchMap(workers => of({ fileUpload: null, stepDataUpdate: stepDataResponse, workers })),
              catchError(error => {
                console.error('⚠️ Error generating workers (continuing anyway):', error);
                return of({ fileUpload: null, stepDataUpdate: stepDataResponse });
              })
            );
          } else {
            return of({ fileUpload: null, stepDataUpdate: stepDataResponse });
          }
        }),
        catchError(error => {
          console.error('❌ Error saving second step data:', error);
          return of(null);
        })
      );
    }
  }

  /**
   * Legacy method - kept for backward compatibility
   */
  /**
   * Saves current state as draft (no validation, just save current progress)
   * Creates project/version if needed, updates if already exists
   */
  saveDraft(): Observable<boolean> {
    const current = this.stateSignal();
    if (!current) {
      console.error('❌ No wizard state to save');
      return of(false);
    }

    console.log('💾 Saving draft...');
    console.log('   Current step:', current.meta.currentStep);
    console.log('   Has project ID:', !!this.createdProjectId);
    console.log('   Has version ID:', !!this.createdVersionId);

    const data = current.data;

    // If we already have project and version, just update
    if (this.createdProjectId && this.createdVersionId) {
      console.log('🔄 Updating existing draft...');
      return this.wizardDataService.updateProjectVersionStepData(
        this.createdProjectId,
        this.createdVersionId,
        data
      ).pipe(
        tap(() => {
          console.log('✅ Draft saved successfully');
          // Update last updated timestamp
          this.stateSignal.set({
            ...current,
            meta: {
              ...current.meta,
              lastUpdatedAt: new Date().toISOString()
            }
          });
        }),
        switchMap(() => of(true)),
        catchError(error => {
          console.error('❌ Error saving draft:', error);
          return of(false);
        })
      );
    }

    // If we don't have project/version yet, create them first
    console.log('✨ Creating new project/version for draft...');
    const projectId = current.meta.projectId;

    return this.wizardDataService.createProject(data, projectId).pipe(
      tap(project => {
        console.log('✅ Project created:', project.id);
        this.createdProjectId = project.id;
      }),
      switchMap(project => 
        this.wizardDataService.createProjectVersion(project.id, data).pipe(
          tap(version => {
            console.log('✅ Version created:', version.id);
            this.createdVersionId = version.id;
            
            // Update state with IDs
            this.stateSignal.set({
              ...current,
              meta: {
                ...current.meta,
                id: version.id,
                lastUpdatedAt: new Date().toISOString()
              }
            });
          })
        )
      ),
      switchMap(() => of(true)),
      catchError(error => {
        console.error('❌ Error creating draft:', error);
        return of(false);
      })
    );
  }

  /**
   * Loads an existing project and its latest version
   * Used when resuming work on an existing project
   */
  loadProjectData(projectId: string, versionId: string): Observable<boolean> {
    console.log('📥 Loading project data:', projectId, versionId);

    return forkJoin({
      project: this.wizardDataService.getProject(projectId),
      version: this.wizardDataService.getProjectVersion(projectId, versionId)
    }).pipe(
      tap(({ project, version }) => {
        console.log('✅ Project loaded:', project);
        console.log('✅ Version loaded:', version);

        // Store IDs for future saves
        this.createdProjectId = project.id;
        this.createdVersionId = version.id;

        // Extract wizard data from stepData
        const wizardData = version.stepData as LetterWizardData;

        // Determine current step based on completed steps
        const completedSteps = this.determineCompletedSteps(wizardData);
        const currentStep = this.determineCurrentStep(completedSteps);

        // Initialize facade with loaded data
        const meta: LetterWizardMeta = {
          id: version.id,
          projectId: project.id,
          currentStep: currentStep,
          completedSteps: completedSteps,
          lastUpdatedAt: version.createdAt,
          versionTag: version.versionTag,
          isActiveVersion: version.isActive,
          status: version.status,
          reviewComment: version.reviewComment ?? null,
          reviewedBy: version.reviewedBy ?? null,
          reviewNotes: version.reviewNotes ?? null
        };

        this.stateSignal.set({ data: wizardData, meta });

        console.log('✅ Wizard state initialized from loaded data');
        console.log('   Current step:', currentStep);
        console.log('   Completed steps:', completedSteps);
      }),
      switchMap(() => of(true)),
      catchError(error => {
        console.error('❌ Error loading project data:', error);
        return of(false);
      })
    );
  }

  /**
   * Determines which steps have been completed based on the data
   */
  private determineCompletedSteps(data: LetterWizardData): LetterWizardStepKey[] {
    const completed: LetterWizardStepKey[] = [];

    console.log('🔍 Determining completed steps from data:', data);

    // Step 1: documentDescription - check if basic info is filled
    // Dane biznesowe są opcjonalne w trakcie pracy (wymagane dopiero przy wysyłce), więc o ukończeniu
    // kroku 1 decyduje sama nazwa projektowa pisma.
    if (data.general?.documentName) {
      completed.push('documentDescription');
      console.log('  ✅ Step 1 (documentDescription) is completed');
    } else {
      console.log('  ⏭️ Step 1 (documentDescription) is NOT completed');
    }

    // Step 2: templateAndSchema - check if template is uploaded
    if (data.templateAndSchema?.templateId) {
      completed.push('templateAndSchema');
      console.log('  ✅ Step 2 (templateAndSchema) is completed');
    } else {
      console.log('  ⏭️ Step 2 (templateAndSchema) is NOT completed');
    }

    // Step 3: configuration - check if any configuration is set
    // Configuration step is considered complete if at least one field is configured
    const configData = data.configuration;
    const hasConfiguration = configData && (
      configData.autoSendEnabled !== undefined ||
      configData.requiresApproval !== undefined ||
      configData.visibilityScope !== undefined ||
      (configData.fieldMappings && configData.fieldMappings.length > 0)
    );
    
    if (hasConfiguration) {
      completed.push('configuration');
      console.log('  ✅ Step 3 (configuration) is completed');
    } else {
      console.log('  ⏭️ Step 3 (configuration) is NOT completed');
    }

    // Step 4: summary - only if all previous steps are done
    // (Not automatically completed - user needs to explicitly finish)

    console.log('📊 Total completed steps:', completed);
    return completed;
  }

  /**
   * Determines the current step based on completed steps
   */
  private determineCurrentStep(completedSteps: LetterWizardStepKey[]): LetterWizardStepKey {
    const allSteps: LetterWizardStepKey[] = ['documentDescription', 'templateAndSchema', 'configuration', 'summary'];
    
    console.log('🎯 Determining current step from completed:', completedSteps);
    
    // Find the first step that's not completed
    for (const step of allSteps) {
      if (!completedSteps.includes(step)) {
        console.log('  ➡️ Current step should be:', step);
        return step;
      }
    }

    // If all steps are completed, go to summary
    console.log('  ➡️ All steps completed, going to summary');
    return 'summary';
  }

  /**
   * Get the created project ID
   */
  getCreatedProjectId(): string | null {
    return this.createdProjectId;
  }

  /**
   * Get the created version ID (for accessing workers and other version-specific data)
   */
  getCreatedVersionId(): string | null {
    return this.createdVersionId;
  }
}
