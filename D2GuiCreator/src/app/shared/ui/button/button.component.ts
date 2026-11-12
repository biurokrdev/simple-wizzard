import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'accent' | 'tertiary' | 'success' | 'danger';
export type ButtonStyle = 'filled' | 'outline' | 'text';
export type ButtonSize = 'small' | 'medium' | 'large';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './button.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './button.component.scss'
})
export class ButtonComponent {
  // Inputs
  variant = input<ButtonVariant>('primary');
  style = input<ButtonStyle>('filled');
  size = input<ButtonSize>('medium');
  disabled = input<boolean>(false);
  type = input<'button' | 'submit' | 'reset'>('button');
  fullWidth = input<boolean>(false);
  icon = input<string | undefined>(undefined);
  iconPosition = input<'left' | 'right'>('left');

  // Outputs
  clicked = output<MouseEvent>();

  onClick(event: MouseEvent): void {
    if (!this.disabled()) {
      this.clicked.emit(event);
    }
  }

  getButtonClasses(): string[] {
    const classes = ['btn'];
    
    classes.push(`btn--${this.variant()}`);
    classes.push(`btn--${this.style()}`);
    classes.push(`btn--${this.size()}`);
    
    if (this.fullWidth()) {
      classes.push('btn--full-width');
    }
    
    if (this.icon()) {
      classes.push('btn--with-icon');
      classes.push(`btn--icon-${this.iconPosition()}`);
    }
    
    return classes;
  }
}
