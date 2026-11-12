
import { Component, EventEmitter, Input, OnInit, Output, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FieldMapping, MapperType } from '../../data-access/letter-wizard.model';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';

@Component({
  selector: 'app-mapping-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, ButtonComponent],
  templateUrl: './mapping-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './mapping-dialog.component.scss'
})
export class MappingDialogComponent implements OnInit {
  @Input() isOpen = false;
  @Input() mapping?: FieldMapping;
  @Input() mapperTypes: MapperType[] = [];
  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<FieldMapping>();

  form!: FormGroup;
  selectedMapperTemplate = signal<string>('');

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      name: [this.mapping?.name || '', Validators.required],
      mapperType: [this.mapping?.mapperType || '', Validators.required],
      mappingJson: [this.mapping?.mappingJson || '', Validators.required]
    });

    // Subscribe to mapper type changes to update JSON template
    this.form.get('mapperType')?.valueChanges.subscribe((mapperKey) => {
      const mapperType = this.mapperTypes.find(mt => mt.key === mapperKey);
      if (mapperType && !this.mapping) {
        // Only set template for new mappings
        this.form.patchValue({ mappingJson: mapperType.templateJson });
        this.selectedMapperTemplate.set(mapperType.templateJson);
      }
    });

    // Set initial template if editing
    if (this.mapping) {
      const mapperType = this.mapperTypes.find(mt => mt.key === this.mapping!.mapperType);
      if (mapperType) {
        this.selectedMapperTemplate.set(mapperType.templateJson);
      }
    }
  }

  onCancel(): void {
    this.close.emit();
  }

  onSave(): void {
    if (this.form.valid) {
      this.save.emit(this.form.value);
    }
  }

  getMapperTypeName(key: string): string {
    return this.mapperTypes.find(mt => mt.key === key)?.name || key;
  }
}
