
import { Component, EventEmitter, Input, Output, signal, OnChanges, SimpleChanges, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Worker } from '../../models/worker.model';
import { WorkerFormData } from '../../models/worker-form.model';
import { ExternalDataService } from '../../services/external-data.service';
import { WorkerTypeDto } from '../../data-access/external-data.model';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';

@Component({
  selector: 'app-worker-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent],
  templateUrl: './worker-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './worker-dialog.component.scss'
})
export class WorkerDialogComponent implements OnInit, OnChanges {
  @Input() isOpen = false;
  @Input() worker?: Worker;
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<WorkerFormData>();

  private readonly externalDataService = inject(ExternalDataService);
  form: FormGroup;
  workerTypes = signal<WorkerTypeDto[]>([]);
  isLoadingTypes = signal(false);

  constructor(private fb: FormBuilder) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      workerType: ['', Validators.required],
      description: [''],
      defaultValue: ['']
    });

    // Nasłuchuj zmian typu workera i generuj szablon
    this.form.get('workerType')?.valueChanges.subscribe(workerType => {
      this.onWorkerTypeChange(workerType);
    });
  }

  ngOnInit(): void {
    this.loadWorkerTypes();
  }

  private loadWorkerTypes(): void {
    this.isLoadingTypes.set(true);
    this.externalDataService.getWorkerTypes().subscribe({
      next: (types: WorkerTypeDto[]) => {
        console.log('✅ Worker types loaded:', types.length);
        this.workerTypes.set(types);
        
        // Ustaw domyślny typ jeśli dostępny
        if (types.length > 0 && !this.worker) {
          this.form.patchValue({ workerType: types[0].code });
        }
        
        this.isLoadingTypes.set(false);
      },
      error: (err: any) => {
        console.error('❌ Error loading worker types:', err);
        this.isLoadingTypes.set(false);
        // Fallback do hardcodowanych typów
        this.workerTypes.set([
          { id: '1', code: 'Podstaw wartość', name: 'Podstaw wartość', isActive: true },
          { id: '2', code: 'Stan', name: 'Stan', isActive: true }
        ]);
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['worker'] && this.worker) {
      this.loadWorkerData();
    } else if (changes['isOpen'] && this.isOpen && !this.worker) {
      const defaultType = this.workerTypes().length > 0 ? this.workerTypes()[0].code : '';
      this.form.reset({
        name: '',
        workerType: defaultType,
        description: '',
        defaultValue: ''
      });
      
      // Wygeneruj szablon dla domyślnego typu
      if (defaultType) {
        this.onWorkerTypeChange(defaultType);
      }
    }
  }

  private loadWorkerData(): void {
    if (!this.worker) return;

    this.form.patchValue({
      name: this.worker.name,
      workerType: this.worker.workerType,
      description: this.worker.configJson,
      defaultValue: ''
    });
  }

  onClose(): void {
    this.close.emit();
  }

  onSave(): void {
    if (this.form.valid) {
      this.save.emit(this.form.value);
      this.form.reset();
    }
  }

  getSelectedTypeDescription(): string {
    const selectedCode = this.form.get('workerType')?.value;
    if (!selectedCode) return '';
    
    const type = this.workerTypes().find(t => t.code === selectedCode);
    return type?.description || '';
  }

  onWorkerTypeChange(workerType: string): void {
    // Nie generuj szablonu podczas edycji
    if (this.isEditMode) return;

    const name = this.form.get('name')?.value || 'nazwa_elementu';
    let template = '';

    if (workerType === 'Podstaw wartość') {
      template = JSON.stringify({
        "replacement-property-expression": `formModel.${name}`,
        "search-for": `<%${name}%>`
      }, null, 2);
    } else if (workerType === 'Stan') {
      template = JSON.stringify({
        "tag-name": name,
        "conditional-expression": `formModel.${name}`
      }, null, 2);
    }

    if (template) {
      this.form.patchValue({ description: template }, { emitEvent: false });
    }
  }

  get isEditMode(): boolean {
    return !!this.worker;
  }
}
