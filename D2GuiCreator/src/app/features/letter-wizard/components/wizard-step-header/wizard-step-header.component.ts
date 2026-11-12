
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-wizard-step-header',
  standalone: true,
  imports: [],
  templateUrl: './wizard-step-header.component.html',
  styleUrls: ['./wizard-step-header.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WizardStepHeaderComponent {
  @Input() title = '';
  @Input() description = '';
  @Input() stepIndex!: number;
  @Input() stepCount!: number;
  @Output() close = new EventEmitter<void>();

  onClose(): void {
    this.close.emit();
  }
}
