import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { D2GlobalBannersComponent } from '../banners/global-banners.component';
import { D2_GUI_CONFIG } from '../config/d2-gui.config';
import { D2FooterComponent } from '../footer/footer.component';
import { D2HeaderComponent } from '../header/header.component';
import { D2ThemeService } from '../theme/d2-theme.service';
import { D2ThemeSwitcherComponent } from '../theme/theme-switcher.component';

/**
 * Globalny shell aplikacji: przyklejona góra (banery + nagłówek), `<router-outlet>` w `<main>`, stopka.
 * Używaj jako komponentu trasy nadrzędnej: `{ path: '', component: D2AppShellComponent, children: [...] }`.
 * Jeśli aplikacja potrzebuje własnej treści w nagłówku (nawigacja, akcje), złóż własny shell
 * z `<d2-global-banners>` + `<d2-header>…</d2-header>` + `<d2-footer>` — klocki są eksportowane osobno.
 */
@Component({
  selector: 'd2-app-shell',
  standalone: true,
  imports: [RouterOutlet, D2GlobalBannersComponent, D2HeaderComponent, D2FooterComponent, D2ThemeSwitcherComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class D2AppShellComponent {
  /** Samo wstrzyknięcie stosuje motyw (zmienne `--d2-color-*` na `<html>`). */
  private readonly theme = inject(D2ThemeService);
  protected readonly showThemeSwitcher = inject(D2_GUI_CONFIG).showThemeSwitcher;
}
