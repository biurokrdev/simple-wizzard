
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output
} from '@angular/core';
import { ButtonComponent } from '../../../../shared/ui/button/button.component';

@Component({
  selector: 'app-wizard-footer',
  standalone: true,
  imports: [ButtonComponent],
  templateUrl: './wizard-footer.component.html',
  styleUrls: ['./wizard-footer.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WizardFooterComponent {
  @Input() canGoBack = false;
  @Input() canGoNext = true;
  @Input() isLastStep = false;
  @Input() isSubmitting = false;
  @Input() isSaving = false;
  @Input() lastSaved: string | null = null;
  /** Tryb podglądu: bez „Zapisz”, „Anuluj” → „Zamknij”, ostatni krok → „Zamknij podgląd”. */
  @Input() readOnly = false;
  /** Przegląd administratora: na ostatnim kroku zamiast „Zamknij podgląd” — „Odeślij do poprawy” i „Zaakceptuj”. */
  @Input() reviewMode = false;

  @Output() approve = new EventEmitter<void>();
  @Output() reject = new EventEmitter<void>();
  @Output() back = new EventEmitter<void>();
  @Output() next = new EventEmitter<void>();
  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  onBack(): void {
    this.back.emit();
  }

  onNext(): void {
    this.next.emit();
  }

  onSave(): void {
    this.save.emit();
  }

  onCancel(): void {
    this.cancel.emit();
  }

  onApprove(): void {
    this.approve.emit();
  }

  onReject(): void {
    this.reject.emit();
  }
}
