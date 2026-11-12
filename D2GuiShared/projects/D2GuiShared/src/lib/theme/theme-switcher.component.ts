import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { D2ThemeService } from './d2-theme.service';

/**
 * Mały przełącznik palety (do nagłówka). Shell pokazuje go, gdy `provideD2Gui({ showThemeSwitcher: true })`.
 * W produkcie motyw zwykle wynika z linii biznesowej użytkownika — wtedy przełącznik wyłączamy,
 * a aplikacja woła `D2ThemeService.setTheme()` sama.
 */
@Component({
  selector: 'd2-theme-switcher',
  standalone: true,
  template: `
    <label class="d2-theme-switcher">
      <span class="d2-theme-switcher__label">Motyw</span>
      <select
        class="d2-theme-switcher__select"
        [value]="themeService.theme().name"
        (change)="themeService.setTheme(selected($event))"
        aria-label="Motyw kolorystyczny aplikacji"
      >
        @for (theme of themeService.themes; track theme.name) {
          <option [value]="theme.name" [selected]="theme.name === themeService.theme().name" [title]="theme.description ?? ''">
            {{ theme.label }}
          </option>
        }
      </select>
      <span class="d2-theme-switcher__swatch" aria-hidden="true"></span>
    </label>
  `,
  styles: `
    .d2-theme-switcher {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      font-size: 13px;
      color: var(--d2-color-muted, #6b6b6b);
    }

    .d2-theme-switcher__select {
      height: 32px;
      padding: 0 8px;
      font: inherit;
      color: var(--d2-color-text, #333333);
      background: var(--d2-color-surface, #ffffff);
      border: 1px solid var(--d2-color-border-strong, #c9c9c9);
      border-radius: 6px;
    }

    .d2-theme-switcher__select:focus-visible {
      outline: 2px solid var(--d2-color-primary, #ff6200);
      outline-offset: 1px;
    }

    .d2-theme-switcher__swatch {
      width: 14px;
      height: 14px;
      border-radius: 50%;
      background: var(--d2-color-primary, #ff6200);
      box-shadow: 0 0 0 2px var(--d2-color-primary-light, #fde3cf);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class D2ThemeSwitcherComponent {
  protected readonly themeService = inject(D2ThemeService);

  protected selected(event: Event): string {
    return (event.target as HTMLSelectElement).value;
  }
}
