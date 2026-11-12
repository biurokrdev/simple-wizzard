import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import { D2_GUI_CONFIG } from '../config/d2-gui.config';
import { D2Theme, D2Theme as Theme, D2_DEFAULT_THEME_NAME, D2_THEMES, d2ThemeCssVariables } from './d2-theme.model';

const STORAGE_KEY = 'd2.theme';

/**
 * Wybór i zastosowanie motywu: wpisuje zmienne `--d2-color-*` na `<html>` i ustawia atrybut
 * `data-d2-theme`. Wybór użytkownika jest pamiętany w `localStorage`; początkowy motyw podaje
 * `provideD2Gui({ theme })`, a dodatkowe palety (np. linii biznesowych) `provideD2Gui({ themes })`.
 *
 * Serwis wystarczy wstrzyknąć (robi to `D2AppShellComponent`), żeby motyw został zastosowany.
 */
@Injectable({ providedIn: 'root' })
export class D2ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly config = inject(D2_GUI_CONFIG);

  /** Wszystkie dostępne palety: wbudowane + z konfiguracji aplikacji (ta sama nazwa = nadpisanie). */
  readonly themes: readonly D2Theme[] = mergeThemes(D2_THEMES, this.config.themes ?? []);

  private readonly themeName = signal<string>(this.resolveInitialName());

  /** Aktywny motyw. */
  readonly theme = computed<D2Theme>(() => this.find(this.themeName()) ?? this.themes[0]);

  constructor() {
    this.apply(this.theme());
  }

  /** Przełącza motyw (nieznana nazwa jest ignorowana) i zapamiętuje wybór. */
  setTheme(name: string): void {
    const theme = this.find(name);
    if (!theme) {
      return;
    }
    this.themeName.set(theme.name);
    this.apply(theme);
    this.persist(theme.name);
  }

  private find(name: string): Theme | undefined {
    return this.themes.find((t) => t.name === name);
  }

  private resolveInitialName(): string {
    const stored = this.readStored();
    if (stored && this.find(stored)) {
      return stored;
    }
    const configured = this.config.theme;
    if (configured && this.find(configured)) {
      return configured;
    }
    return this.find(D2_DEFAULT_THEME_NAME) ? D2_DEFAULT_THEME_NAME : this.themes[0].name;
  }

  private apply(theme: D2Theme): void {
    const root = this.document.documentElement;
    if (!root) {
      return;
    }
    for (const [variable, value] of Object.entries(d2ThemeCssVariables(theme))) {
      root.style.setProperty(variable, value);
    }
    root.setAttribute('data-d2-theme', theme.name);
  }

  private readStored(): string | null {
    try {
      return this.document.defaultView?.localStorage?.getItem(STORAGE_KEY) ?? null;
    } catch {
      return null;
    }
  }

  private persist(name: string): void {
    try {
      this.document.defaultView?.localStorage?.setItem(STORAGE_KEY, name);
    } catch {
      // brak localStorage (tryb prywatny, testy) — wybór działa do przeładowania
    }
  }
}

function mergeThemes(builtIn: readonly D2Theme[], extra: readonly D2Theme[]): readonly D2Theme[] {
  const byName = new Map<string, D2Theme>();
  for (const theme of [...builtIn, ...extra]) {
    byName.set(theme.name, theme);
  }
  return [...byName.values()];
}
