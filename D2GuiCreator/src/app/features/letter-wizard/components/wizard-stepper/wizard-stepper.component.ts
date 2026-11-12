
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';
import { WizardStepperItem } from './wizard-stepper.model';

@Component({
  selector: 'app-wizard-stepper',
  standalone: true,
  imports: [],
  templateUrl: './wizard-stepper.component.html',
  styleUrls: ['./wizard-stepper.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WizardStepperComponent {
  @Input() steps: WizardStepperItem[] = [];

  @Output() stepClicked = new EventEmitter<WizardStepperItem>();

  onStepClick(step: WizardStepperItem): void {
    if (step.status === 'todo') {
      return;
    }
    this.stepClicked.emit(step);
  }

  trackByKey(_: number, step: WizardStepperItem): string {
    return step.key;
  }
}
