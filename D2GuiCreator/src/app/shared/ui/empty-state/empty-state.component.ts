import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonComponent } from '../button/button.component';

/** Ikony stanów pustych / błędów — rysowane inline (SVG), kolory z motywu. */
export type EmptyStateIcon = 'folder' | 'search' | 'error' | 'inbox' | 'users' | 'check';

/**
 * Stan pusty strony lub sekcji: ikona w kolorowym kole, tytuł, opis, akcja główna i miejsce na
 * dodatkowe przyciski (`<ng-content>`). Wariant `tone="error"` dla komunikatów o niepowodzeniu.
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [ButtonComponent],
  templateUrl: './empty-state.component.html',
  styleUrls: ['./empty-state.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  @Input() icon: EmptyStateIcon = 'folder';
  @Input() tone: 'neutral' | 'error' = 'neutral';
  /** Rozmiar: `page` (duży, środek strony) albo `section` (mniejszy, wewnątrz panelu). */
  @Input() size: 'page' | 'section' = 'page';
  @Input() title = '';
  @Input() message?: string;
  @Input() description?: string;
  @Input() actionLabel?: string;
  @Output() actionClick = new EventEmitter<void>();

  onActionClick(): void {
    this.actionClick.emit();
  }
}
